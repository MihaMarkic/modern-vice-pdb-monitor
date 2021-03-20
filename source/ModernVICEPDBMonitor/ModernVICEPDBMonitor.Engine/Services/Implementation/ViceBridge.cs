using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
        readonly byte[] buffer = new byte[20000];
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
                            Loop(socket);
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
            while (socket.Available > 0)
            {
                socket.Receive(buffer);
            }
        }
        void Loop(Socket socket)
        {
            if (commands.TryDequeue(out var command))
            {
                ConsumeData(socket);
                switch (command)
                {
                    case ViceBinnaryCommand binaryCommand:
                        SendBinaryCommand(binaryCommand);
                        break;
                    case ViceTextCommand textCommand:
                        SendTextCommand(textCommand);
                        break;
                    default:
                        throw new Exception($"Invalid command type {command?.GetType()}");
                }
            }
            else if (socket.Available > 0)
            {
                // handle error here
            }
        }

        void SendBinaryCommand(ViceBinnaryCommand command)
        {

        }

        void SendTextCommand(ViceTextCommand command)
        {

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
