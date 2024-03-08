namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;
public interface IProfiler: IAsyncDisposable
{
    event EventHandler? IsActiveChanged;
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    bool IsActive { get; }
}
