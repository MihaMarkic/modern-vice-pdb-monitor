using System;
using System.Threading;
using System.Threading.Tasks;
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
        IsActive = true;
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
            executionStatusViewModel.IsSteppingInto = false;
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
            IsActive = false;
            StartLine = null;
        }
    }
}
