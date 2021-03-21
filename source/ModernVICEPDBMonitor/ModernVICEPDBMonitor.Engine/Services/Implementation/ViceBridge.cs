using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModernVicePdbMonitor.Engine.Services.Abstract;

namespace ModernVicePdbMonitor.Engine.Services.Implementation
{
    public sealed class ViceBridge: IViceBridge
    {
        readonly ILogger<ViceBridge> logger;
        CancellationTokenSource? cts;
        Task? loop;
        TaskCompletionSource? tcs;
        readonly ArrayPool<byte> byteArrayPool = ArrayPool<byte>.Shared;
        readonly ConcurrentQueue<ViceCommand> commands = new ConcurrentQueue<ViceCommand>();
        public Task? RunnerTask => tcs?.Task;
        public ViceBridge(ILogger<ViceBridge> logger)
        {
            this.logger = logger;
        }
        async Task WaitForPort(int port, CancellationToken ct = default)
        {
            bool isAvailable;
            do
            {
                ct.ThrowIfCancellationRequested();
                var properties = IPGlobalProperties.GetIPGlobalProperties();
                var info = properties.GetActiveTcpListeners();

                isAvailable = info.Any(tl => tl.Port == port);
                if (!isAvailable)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
            } while (!isAvailable);
        }
        public bool IsRunning => cts is not null && loop is not null;
        public void Start(IPAddress address, int port = 6510)
        {
            if (!IsRunning)
            {
                logger.LogDebug("Start called");
                tcs = new TaskCompletionSource();
                cts = new CancellationTokenSource();
                var task = Task.Factory.StartNew(
                    () => StartAsync(address, port, cts.Token), 
                    cancellationToken: cts.Token, 
                    creationOptions: TaskCreationOptions.LongRunning,
                    scheduler: TaskScheduler.Default);
                loop = task.Unwrap();
            }
            else
            {
                logger.LogInformation("Already running");
            }

        }
        async Task StartAsync(IPAddress address, int port, CancellationToken ct)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                while (true)
                {
                    logger.LogInformation("Starting");
                    try
                    {
                        logger.LogDebug("Waiting for available port");
                        await WaitForPort(port, ct).ConfigureAwait(false);
                        logger.LogDebug("Port acquired");
                        socket.Connect("localhost", 6510);
                        logger.LogDebug("Port connected");
                        while (socket.Connected)
                        {
                            await LoopAsync(socket, ct);
                            await Task.Delay(500, ct).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogInformation("Finishing loop");
                        tcs!.SetResult();
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Unknown exception occured");
                    }
                    finally
                    {
                        logger.LogInformation("Ending");
                    }
                }
            }
            finally
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket.Dispose();
            }
        }
        void ConsumeData(Socket socket)
        {
            // Try to consume anything before sending commands...
            using (var buffer = byteArrayPool.GetBuffer(20_000))
            {
                while (socket.Available > 0)
                {
                    socket.Receive(buffer.Data);
                }
            }
        }
        async Task LoopAsync(Socket socket, CancellationToken ct)
        {
            if (commands.TryDequeue(out var command))
            {
                ConsumeData(socket);
                switch (command)
                {
                    case ViceBinnaryCommand binaryCommand:
                        await SendBinaryCommandAsync(socket, binaryCommand, ct);
                        await Task.Delay(10);
                        var binaryResponse = await GetBinaryResponseAsync(socket, ct);
                        try
                        {
                            binaryCommand.SetResult(binaryResponse);
                        }
                        finally
                        {
                            binaryResponse?.Dispose();
                        }
                        break;
                    case IViceTextCommand textCommand:
                        await ProcessTextCommandAsync(socket, textCommand, ct);
                        break;
                    default:
                        throw new Exception($"Invalid command type {command?.GetType()}");
                }
            }
            else if (socket.Available > 0)
            {
            }
        }

        async Task ProcessTextCommandAsync(Socket socket, IViceTextCommand command, CancellationToken ct)
        {
            await SendTextCommandAsync(socket, command.Content, ct);
            await Task.Delay(10);
            switch (command.Mode)
            {
                //case ViceCommandMode.ThrowAwayResults:
                //    GetReply();
                //    if (lastCommand.textDelegate != null)
                //    {
                //        lastCommand.dispatch.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, lastCommand.textDelegate, null, lastCommand.userData);
                //    }
                //    break;
                //case CommandStruct.eMode.DoCommandReturnResults:
                //    string reply = GetReply();
                //    lastCommand.dispatch.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, lastCommand.textDelegate, reply, lastCommand.userData);
                //    break;
                //case CommandStruct.eMode.DoCommandThenExit:
                //    GetReply();
                //    SendCommand("x");
                //    break;
                //case CommandStruct.eMode.DoCommandOnly:
                //    ConsumeData();
                //    break; //don't a single thing
                //case CommandStruct.eMode.DoCommandFireCallback:
                //    if (lastCommand.textDelegate != null)
                //    {
                //        lastCommand.dispatch.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, lastCommand.textDelegate, null, lastCommand.userData);
                //    }
                //    break;
                default:
                    throw new Exception($"Unknown text command mode {command?.Mode}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="ct"></param>
        /// <returns>An instance of <see cref="ManagedBuffer"/> that has to be disposed after use.</returns>
        async Task<ManagedBuffer?> GetBinaryResponseAsync(Socket socket, CancellationToken ct)
        {
            const int MaxBufferSize = 64 * 1024;
            using (var headerBuffer = byteArrayPool.GetBuffer(6))
            {
                await ReadByteArrayAsync(socket, headerBuffer, ct);
                if (headerBuffer.Data[0] != 0x2 || headerBuffer.Data[5] != 0x0)
                {
                    return null;
                }
                int responseLength = headerBuffer.Data[1] + (headerBuffer.Data[2] << 8) + (headerBuffer.Data[3] << 16) + (headerBuffer.Data[4] << 24);
                if (responseLength > MaxBufferSize- headerBuffer.Size)
                {
                    // too much data
                    return null;
                }
                var buffer = byteArrayPool.GetBuffer(responseLength);
                await ReadByteArrayAsync(socket, buffer, ct);
                return buffer;
            }
        }
        async Task SendBinaryCommandAsync(Socket socket, ViceBinnaryCommand command, CancellationToken ct)
        {
            int bufferLength = command.Content.Length + 3;
            using (var buffer = byteArrayPool.GetBuffer(bufferLength))
            {
                buffer.Data[0] = 0x2;
                buffer.Data[1] = (byte)command.Content.Length;
                Buffer.BlockCopy(command.Content, 0, buffer.Data, 2, command.Content.Length);
                buffer.Data[bufferLength - 1] = 0x0;
                await SendByteArrayAsync(socket, buffer.Data, buffer.Size, ct);
            }
        }
        async Task ReadByteArrayAsync(Socket socket, ManagedBuffer buffer, CancellationToken ct = default)
        {
            int i = 0;
            var dataSpan = buffer.Data.AsMemory();
            do
            {
                i += await socket.ReceiveAsync(dataSpan[i..buffer.Size], SocketFlags.None);
            }
            while (i < buffer.Size);
        }
        async Task SendByteArrayAsync(Socket socket, byte[] data, int length, CancellationToken ct)
        {
            int i = 0;
            var dataSpan = data.AsMemory();
            do
            {
                int sent = await socket.SendAsync(dataSpan[i..length], SocketFlags.None, ct);
                if (sent > 0)
                {
                    i += sent;
                }
                else
                {
                    await Task.Delay(10, ct);
                }
                ct.ThrowIfCancellationRequested();
            }
            while (i < length);
        }

        async Task SendTextCommandAsync(Socket socket, string command, CancellationToken ct)
        {
            // Add padding to avoid the VICE monitor command truncation bug
            string text = $"{command}                                                                           \n";
            byte[] data = Encoding.ASCII.GetBytes(text);
            await SendByteArrayAsync(socket, data, data.Length, ct);
        }

        public async ValueTask DisposeAsync()
        {
            if (cts is not null && loop is not null)
            {
                logger.LogDebug("Dispose async");
                cts.Cancel();
                try
                {
                    await loop;
                }
                catch (OperationCanceledException)
                { }
                logger.LogDebug("Disposed async");
                cts = null;
                loop = null;
            }
            else
            {
                logger.LogDebug("Nothing to dispose async");
            }
        }

        public void Dispose()
        {
            logger.LogDebug("Dispose");
            cts?.Cancel();
        }
    }
}
