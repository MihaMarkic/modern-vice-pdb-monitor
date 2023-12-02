using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;
public class HighLevelDebugStepper : DebugStepper, IDebugStepper
{
    public HighLevelDebugStepper(IViceBridge viceBridge, ILogger<HighLevelDebugStepper> logger, IDispatcher dispatcher, 
        ExecutionStatusViewModel executionStatusViewModel) : base(viceBridge, logger, dispatcher, executionStatusViewModel)
    {
    }
    public async Task StepIntoAsync(PdbLine? line, CancellationToken ct = default)
    {
        StartLine = line;
        executionStatusViewModel.IsSteppingOver = false;
        executionStatusViewModel.IsSteppingInto = true;
        SteppingStart = DateTimeOffset.Now;
        IsActive = true;
        try
        {
            while (IsActive)
            {
                ct.ThrowIfCancellationRequested();
                PrepareForContinue();
                await AtomicStepIntoAsync(ct);
                await ContinueTask!;
            }
        }
        finally
        {
            executionStatusViewModel.IsSteppingInto = false;
            SteppingStart = null;
            IsActive = false;
            StartLine = null;
        }
    }
    public async Task StepOverAsync(PdbLine? line, CancellationToken ct = default)
    {
        StartLine = line;
        executionStatusViewModel.IsSteppingOver = true;
        executionStatusViewModel.IsSteppingInto = false;
        IsActive = true;
        SteppingStart = DateTimeOffset.Now;
        try
        {
            while (IsActive)
            {
                ct.ThrowIfCancellationRequested();
                PrepareForContinue();
                await AtomicStepOverAsync(ct);
                await ContinueTask!;
            }
        }
        finally
        {
            executionStatusViewModel.IsSteppingOver = false;
            SteppingStart = null;
            IsActive = false;
            StartLine = null;
        }
    }
    public override async Task ContinueAsync(PdbLine? line, CancellationToken ct = default)
    {
        StartLine = line;
        executionStatusViewModel.IsSteppingOver = true;
        executionStatusViewModel.IsSteppingInto = false;
        IsActive = true;
        SteppingStart = DateTimeOffset.Now;
        try
        {
            int id = 0;
            while (IsActive)
            {
                ct.ThrowIfCancellationRequested();
                PrepareForContinue();
                await ExitViceMonitorAsync();
                await ContinueTask!;
                id++;
            }
        }
        finally
        {
            executionStatusViewModel.IsSteppingOver = false;
            IsActive = false;
            StartLine = null;
        }
    }
}
