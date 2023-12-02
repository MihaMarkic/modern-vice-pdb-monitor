namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract;

public interface IProjectPrgFileWatcher
{
    void Start(string path, string filter);
    void Stop();
}
