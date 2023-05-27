using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;
internal class AssemblyDebugStepper : DebugStepper, IDebugStepper
{
    public AssemblyDebugStepper(IViceBridge viceBridge, ILogger<AssemblyDebugStepper> logger, IDispatcher dispatcher, 
        ExecutionStatusViewModel executionStatusViewModel) : base(viceBridge, logger, dispatcher, executionStatusViewModel)
    {
    }
    public async Task StepIntoAsync(PdbLine? line, CancellationToken ct = default) => await AtomicStepIntoAsync(ct);

    public async Task StepOverAsync(PdbLine? line, CancellationToken ct = default) =>await AtomicStepOverAsync(ct);
    public async override Task ContinueAsync(PdbLine? line, CancellationToken ct = default)
    {
        await ExitViceMonitorAsync();
    }
}
