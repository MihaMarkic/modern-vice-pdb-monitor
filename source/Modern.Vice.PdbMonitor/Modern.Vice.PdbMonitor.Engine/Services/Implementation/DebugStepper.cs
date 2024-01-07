using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;
public abstract class DebugStepper: DisposableObject
{
#if DEBUG
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
#else
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
#endif
    readonly IViceBridge viceBridge;
    readonly ILogger logger;
    readonly IDispatcher dispatcher;
    protected readonly ExecutionStatusViewModel executionStatusViewModel;
    public bool IsActive { get; protected set; }
    public PdbLine? StartLine { get; protected set; }
    /// <inheritdoc/>
    public DateTimeOffset? SteppingStart { get; protected set; }
    TaskCompletionSource? continueTcs;
    public DebugStepper(IViceBridge viceBridge, ILogger logger, IDispatcher dispatcher, ExecutionStatusViewModel executionStatusViewModel)
    {
        this.viceBridge = viceBridge;
        this.logger = logger;
        this.dispatcher = dispatcher;
        this.executionStatusViewModel = executionStatusViewModel;
        executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
    }
    public bool IsTimeout(DateTimeOffset current)
    {
        return SteppingStart.HasValue && (current - SteppingStart.Value) > Timeout;
    }
    void ExecutionStatusViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ExecutionStatusViewModel.IsDebuggingPaused) when executionStatusViewModel.IsDebuggingPaused:
                continueTcs?.TrySetResult();
                break;
        }
    }

    internal async Task AtomicStepIntoAsync(CancellationToken ct = default)
    {
        ushort instructionsNumber = 1;
        var command = viceBridge.EnqueueCommand(new AdvanceInstructionCommand(StepOverSubroutine: false, instructionsNumber));
        await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command);
    }

    internal async Task AtomicStepOverAsync(CancellationToken ct = default)
    {
        ushort instructionsNumber = 1;
        var command = viceBridge.EnqueueCommand(new AdvanceInstructionCommand(StepOverSubroutine: true, instructionsNumber));
        await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command);
    }
    public abstract Task ContinueAsync(PdbLine? line, CancellationToken ct = default);
    public async Task ExitViceMonitorAsync()
    {
        var command = viceBridge.EnqueueCommand(new ExitCommand(), resumeOnStopped: false);
        await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command);
    }
    protected void PrepareForContinue()
    {
        continueTcs = new TaskCompletionSource();
    }
    public void Continue()
    {
        //if (continueTcs is null)
        //{
        //    throw new Exception("Debugger Step continuation is not ready");
        //}
        //continueTcs.SetResult();
    }
    protected Task? ContinueTask => continueTcs?.Task;

    public void Stop()
    {
        IsActive = false;
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            executionStatusViewModel.PropertyChanged -= ExecutionStatusViewModel_PropertyChanged;
        }
        base.Dispose(disposing);
    }
}
