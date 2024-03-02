namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;
public interface IProfiler: IAsyncDisposable
{
    event EventHandler? IsActiveChanged;
    Task StartAsync();
    Task StopAsync();
    bool IsActive { get; }
}
