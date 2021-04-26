using System;

namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract
{
    public interface IProjectPdbFileWatcher
    {
        void Start(string path, string filter);
        void Stop();
    }
}
