using System.Threading;
using System.Threading.Tasks;
using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract;
public interface IDebugStepper
{
    bool IsActive { get; }
    PdbLine? StartLine { get; }
    void Stop();
    void Continue();
    Task ContinueAsync(PdbLine? line, CancellationToken ct = default);
    Task ExitViceMonitorAsync();
    Task StepOverAsync(PdbLine? line, CancellationToken ct = default);
    Task StepIntoAsync(PdbLine? line, CancellationToken ct = default);
}
