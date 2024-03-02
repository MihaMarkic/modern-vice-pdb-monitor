namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;
public interface IProfiler: IAsyncDisposable
{
    event EventHandler? IsActiveChanged;
    void Start();
    Task StopAsync();
    bool IsActive { get; }
}
