using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;
public class AssemblyDebugStepper : DebugStepper, IDebugStepper
{
    public AssemblyDebugStepper(IViceBridge viceBridge, ILogger<AssemblyDebugStepper> logger, IDispatcher dispatcher, 
        ExecutionStatusViewModel executionStatusViewModel) : base(viceBridge, logger, dispatcher, executionStatusViewModel)
    {
    }
    public async Task StepIntoAsync(PdbLine? line, CancellationToken ct = default)
    {
        IsActive = true;
        try
        {
            await AtomicStepIntoAsync(ct);
        }
        finally
        {
            IsActive = false;
        }
    }

    public async Task StepOverAsync(PdbLine? line, CancellationToken ct = default)
    {
        IsActive = true;
        try
        {
            await AtomicStepOverAsync(ct);
        }
        finally
        {
            IsActive = false;
        }
    }
    public async override Task ContinueAsync(PdbLine? line, CancellationToken ct = default)
    {
        await ExitViceMonitorAsync();
    }
}
