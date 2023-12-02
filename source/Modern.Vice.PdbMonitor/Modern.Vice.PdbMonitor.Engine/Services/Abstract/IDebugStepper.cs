using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract;
public interface IDebugStepper
{
    bool IsActive { get; }
    PdbLine? StartLine { get; }
    /// <summary>
    /// Defines start of the stepping operation.
    /// </summary>
    /// <remarks>Used to implement time outs in <see cref="HighLevelDebugStepper"/>.</remarks>
    DateTimeOffset? SteppingStart { get; }
    bool IsTimeout(DateTimeOffset current);
    void Stop();
    void Continue();
    Task ContinueAsync(PdbLine? line, CancellationToken ct = default);
    Task ExitViceMonitorAsync();
    Task StepOverAsync(PdbLine? line, CancellationToken ct = default);
    Task StepIntoAsync(PdbLine? line, CancellationToken ct = default);
}
