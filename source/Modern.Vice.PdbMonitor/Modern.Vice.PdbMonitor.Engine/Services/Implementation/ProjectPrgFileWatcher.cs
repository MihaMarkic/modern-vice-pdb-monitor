using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation
{
    /// <summary>
    /// Watches for PDB file changes
    /// </summary>
    public sealed class ProjectPrgFileWatcher : DisposableObject, IProjectPrgFileWatcher
    {
        readonly ILogger<ProjectPrgFileWatcher> logger;
        readonly IDispatcher dispatcher;
        FileSystemWatcher? watcher;
        public ProjectPrgFileWatcher(ILogger<ProjectPrgFileWatcher> logger, IDispatcher dispatcher)
        {
            this.logger = logger;
            this.dispatcher = dispatcher;
        }
        public void Start(string path, string filter)
        {
            if (watcher is null)
            {
                watcher = new FileSystemWatcher();
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Changed += Watcher_Changed;
            }
            watcher.EnableRaisingEvents = false;
            watcher.Path = path;
            watcher.Filter = filter;
            watcher.EnableRaisingEvents = true;
            logger.LogDebug("Started watching changes for file {File} in {Directory}", watcher.Filter, watcher.Path);
        }

        public void Stop()
        {
            if (watcher is not null)
            {
                watcher.EnableRaisingEvents = false;
                logger.LogDebug("Stopped watching changes for file {File} in {Directory}", watcher.Path, watcher.Filter);
            }
        }

        void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    dispatcher.Dispatch(new PrgFileChangedMessage());
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                watcher?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
