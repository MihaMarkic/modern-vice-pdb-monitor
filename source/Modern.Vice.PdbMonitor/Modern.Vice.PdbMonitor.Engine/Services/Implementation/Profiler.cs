using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Xml.Schema;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;
public sealed class Profiler : IProfiler
{
    readonly ILogger<Profiler> logger;
    readonly IViceBridge viceBridge;
    readonly IDispatcher dispatcher;
    readonly Globals globals;
    public bool IsActive { get; private set; }
    public event EventHandler? IsActiveChanged;
    Task? loop;
    CancellationTokenSource? ctsLoop;
    TaskScheduler uiTaskScheduler;
    public Profiler(ILogger<Profiler> logger, IViceBridge viceBridge, IDispatcher dispatcher,
        Globals globals)
    {
        this.logger = logger;
        this.viceBridge = viceBridge;
        this.dispatcher = dispatcher;
        this.globals = globals;
        uiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
    }

    TaskCompletionSource? stoppedTcs;
    public async Task StartAsync()
    {
        if (!IsActive)
        {
            if (globals.Project is null)
            {
                logger.LogError("Can't run profiler when there is no project");
                return;
            }
            IsActive = true;
            OnIsActiveChanged();
            viceBridge.ViceResponse += ViceBridge_ViceResponse;
            viceBridge.ConnectedChanged += ViceBridge_ConnectedChanged;

            Debug.WriteLine("Starting profiler");
            var command = viceBridge.EnqueueCommand(
                new AutoStartCommand(runAfterLoading: false, 0, globals.Project.FullPrgPath!), resumeOnStopped: false);
            await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command);

            ctsLoop = new CancellationTokenSource();
            loop = Task.Factory.StartNew(
                async () => await LoopAsync(ctsLoop.Token),
                ctsLoop.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
            stoppedTcs = new TaskCompletionSource();
            _ = loop.ContinueWith(c =>
            {
                viceBridge.ViceResponse -= ViceBridge_ViceResponse;
                viceBridge.ConnectedChanged -= ViceBridge_ConnectedChanged;
                IsActive = false;
                OnIsActiveChanged();
                stoppedTcs.SetResult();
                Debug.WriteLine("Profiles process done");
                return Task.CompletedTask;
            }, uiTaskScheduler);
        }
    }

    TaskCompletionSource? tcs;
    private async Task LoopAsync(CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();
        int commands = 0;
        double averagePerCommand = 0;
        Debug.WriteLine("Profiler loop started");
        try
        {
            while (!ct.IsCancellationRequested)
            {
                //Debug.WriteLine($"Step into {commands}");
                var c = viceBridge.EnqueueCommand(
                    new AdvanceInstructionCommand(StepOverSubroutine: false, NumberOfInstructions: 1));
                try
                {
                    tcs = new TaskCompletionSource();
                    var response = await c.Response.AwaitWithTimeoutAsync(TimeSpan.FromSeconds(1));
                    //Debug.WriteLine("Step into done");
                    await tcs.Task;
                    tcs = null;
                    //Debug.WriteLine("Pause done");
                    //if (response?.ErrorCode != ErrorCode.OK)
                    //{
                    //    logger.LogError("Got error code {code}", response?.ErrorCode);
                    //}
                    //Debug.WriteLine("Get registers");
                    //var r = viceBridge.EnqueueCommand(getRegisters);
                    //await r.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, r, UpdateRegistersFromResponseAsync, ct: ct);
                    //Debug.WriteLine("Get registers done");
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
                averagePerCommand = sw.ElapsedMilliseconds / (double)commands;
                if (commands % 100 == 0)
                {
                    Debug.WriteLine($"Average for {commands} commands is {averagePerCommand:#,##0.00}ms");
                }
                //Debug.WriteLine($"Loop {commands} ended");
            }
            var resumeCommand = viceBridge.EnqueueCommand(new ExitCommand());
            await resumeCommand.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, resumeCommand);
            Debug.WriteLine("Profiler resummed VICE");
        }
        finally
        {
            Debug.WriteLine("Profiler loop stopped");
        }
    }
    Task UpdateRegistersFromResponseAsync(RegistersResponse response)
    {
        return Task.CompletedTask;
    }

    private void ViceBridge_ConnectedChanged(object? sender, ConnectedChangedEventArgs e)
    {
    }

    private void ViceBridge_ViceResponse(object? sender, ViceResponseEventArgs e)
    {
        if (e.Response is StoppedResponse)
        {
            tcs?.TrySetResult();
        }
        //Debug.WriteLine($"Response {e.Response.GetType().Name}");
    }

    public async Task StopAsync()
    {
        if (IsActive && stoppedTcs is not null && ctsLoop is not null)
        {
            ctsLoop.Cancel();
            await stoppedTcs.Task;
        }
    }

    void OnIsActiveChanged() => IsActiveChanged?.Invoke(this, EventArgs.Empty);

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
