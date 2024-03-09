using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;
using static Compiler.Oscar64.Services.Implementation.AsmParser;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;
public sealed class Profiler : IProfiler
{
    const int InstructionsPerStep = 50;
    readonly ILogger<Profiler> logger;
    readonly IViceBridge viceBridge;
    readonly IDispatcher dispatcher;
    readonly RegistersMapping registersMapping;
    readonly Globals globals;
    public bool IsActive { get; private set; }
    public event EventHandler? IsActiveChanged;
    byte pcRegisterId;
    Task? loop;
    CancellationTokenSource? ctsLoop;
    TaskScheduler uiTaskScheduler;
    public Profiler(ILogger<Profiler> logger, IViceBridge viceBridge, IDispatcher dispatcher,
        RegistersMapping registersMapping, Globals globals)
    {
        this.logger = logger;
        this.viceBridge = viceBridge;
        this.dispatcher = dispatcher;
        this.registersMapping = registersMapping;
        this.globals = globals;
        uiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
    }

    TaskCompletionSource? loopStoppedTcs;
    public async Task StartAsync(CancellationToken ct)
    {
        if (!IsActive)
        {
            if (globals.Project is null)
            {
                logger.LogError("Can't run profiler when there is no project");
                return;
            }
            if (globals.Project.FullPrgPath is null)
            {
                logger.LogError("Project's .prg path is not set");
                return;
            }
            if (globals.Project.StartAddress is null)
            {
                logger.LogError("Project's entry address is unknown");
                return;
            }
            IsActive = true;
            OnIsActiveChanged();
            viceBridge.ConnectedChanged += ViceBridge_ConnectedChanged;

            Debug.WriteLine("Starting profiler");
            loopStoppedTcs = new TaskCompletionSource();
            ctsLoop = new CancellationTokenSource();
            bool isAppStarted = await StartAppAsync(globals.Project.FullPrgPath, globals.Project.StartAddress.Value, ct);
            if (!isAppStarted || ctsLoop.IsCancellationRequested)
            {
                logger.LogError(isAppStarted ? "Profiling cancelled": "Failed starting app");
                loopStoppedTcs.SetResult();
                IsActive = false;
                OnIsActiveChanged();
                viceBridge.ConnectedChanged -= ViceBridge_ConnectedChanged;
                return;
            }

            // creates a channel for consumer/producer of data
            var channel = Channel.CreateUnbounded<ProfilingData>(new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = false
            });
            var reader = ProfilingDataConsumer(channel.Reader);
            Debug.WriteLine("App ready for profiling");
            loop = Task.Factory.StartNew(
                async () => await LoopAsync(channel.Writer, ctsLoop.Token),
                ctsLoop.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
            _ = loop.ContinueWith(async c =>
            {
                Debug.WriteLine("Shutting down channel");
                channel.Writer.TryComplete();
                Debug.WriteLine("Waiting for consumer");
                await reader;
                Debug.WriteLine("Consumer done");
                viceBridge.ConnectedChanged -= ViceBridge_ConnectedChanged;
                IsActive = false;
                OnIsActiveChanged();
                loopStoppedTcs.SetResult();
                Debug.WriteLine("Profiles process done");
                return Task.CompletedTask;
            }, uiTaskScheduler);
         }
    }

    internal async Task<bool> StartAppAsync(string fullPrgPath, ushort startAddress, CancellationToken ct)
    {
        Debug.WriteLine("Wait for registers");
        await registersMapping.Initialized;
        var pc = registersMapping.GetRegisterId(Register6510.PC);
        if (pc is null)
        {
            logger.LogError("Couldn't retrieve PC register");
            return false;
        }
        pcRegisterId = pc.Value;
        Debug.WriteLine($"Setting temporary checkpoint at {startAddress:X4}");
        var startCheckpoint = viceBridge.EnqueueCommand(
            new CheckpointSetCommand(
                StartAddress: startAddress,
                EndAddress: (ushort)(startAddress + 1),
                StopWhenHit: true,
                Enabled: true,
                CpuOperation.Exec,
                Temporary: true));
        var startCheckpointResponse = await startCheckpoint.Response;
        Debug.WriteLine("Checkpoint awaited");
        Debug.WriteLine("Starting app");
        var waitForCheckpoint = viceBridge.WaitForUnboundResponse<CheckpointInfoResponse>(ct);
        var command = viceBridge.EnqueueCommand(
    new AutoStartCommand(runAfterLoading: true, 0, fullPrgPath), resumeOnStopped: false);
        await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command);
        Debug.WriteLine("App start done");
        var checkpointInfoResponse = await waitForCheckpoint;
        Debug.WriteLine("Awaited for checkpoint");
        return startCheckpointResponse.Response?.CheckpointNumber == checkpointInfoResponse.CheckpointNumber;
    }

    private async ValueTask ProfilingDataConsumer(ChannelReader<ProfilingData> reader)
    {
        await foreach (ProfilingData data in reader.ReadAllAsync().ConfigureAwait(false))
        {
            //await Task.Delay(1);
            Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId} Received {data.index}:{data.PC}");
        }
        Debug.WriteLine("Consumer loop ended");
    }
    ulong profilingDataIndex;
    private async Task LoopAsync(ChannelWriter<ProfilingData> writer, CancellationToken ct)
    {
        int commands = 0;
        double averagePerCommand = 0;
        Stopwatch sw = Stopwatch.StartNew();
        //await viceBridge.EnqueueCommand(new ExitCommand()).Response;
        Debug.WriteLine("Profiler loop started");
        profilingDataIndex = 0;
        EventHandler<ViceResponseEventArgs> unboundEventsHandler = (s, e) =>
        {
            if (e.Response is RegistersResponse response)
            {
                ushort address = response.Items.Single(i => i.RegisterId == pcRegisterId).RegisterValue;
                writer.TryWrite(new ProfilingData(profilingDataIndex++, address));
            }
        };
        viceBridge.ViceResponse += unboundEventsHandler;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                //executionStoppedTcs = new TaskCompletionSource();
                var c = viceBridge.EnqueueCommand(
                    new AdvanceInstructionCommand(StepOverSubroutine: false, NumberOfInstructions: InstructionsPerStep));
                try
                {
                    //var response = await c.Response.AwaitWithTimeoutAsync(TimeSpan.FromSeconds(1));
                    //Debug.WriteLine("Step into done");
                    //await executionStoppedTcs.Task;
                    //executionStoppedTcs = null;
                    await viceBridge.WaitForUnboundResponse<StoppedResponse>();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Profiler step into");
                    return;
                }
                commands++;
                averagePerCommand = sw.ElapsedMilliseconds / (double)(commands * InstructionsPerStep);
                if (commands % 100 == 0)
                {
                    Debug.WriteLine($"Average for {commands * InstructionsPerStep:#,##0} commands is {averagePerCommand:#,##0.0000}ms");
                }
                //await viceBridge.EnqueueCommand(new ExitCommand()).Response;
                //Debug.WriteLine($"Total loop cycle {sw.ElapsedTicks:#,##0}");
                //Debug.WriteLine($"Loop {commands} ended");
            }
            var resumeCommand = viceBridge.EnqueueCommand(new ExitCommand());
            await resumeCommand.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, resumeCommand);
            Debug.WriteLine("Profiler resummed VICE");
        }
        finally
        {
            viceBridge.ViceResponse -= unboundEventsHandler;
            Debug.WriteLine("Profiler loop stopped");
        }
    }

    private void ViceBridge_ConnectedChanged(object? sender, ConnectedChangedEventArgs e)
    {
    }

    public async Task StopAsync()
    {
        if (IsActive && loopStoppedTcs is not null && ctsLoop is not null)
        {
            ctsLoop.Cancel();
            await loopStoppedTcs.Task;
        }
    }

    void OnIsActiveChanged() => IsActiveChanged?.Invoke(this, EventArgs.Empty);

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}

record ProfilingData(ulong index, ushort PC);
