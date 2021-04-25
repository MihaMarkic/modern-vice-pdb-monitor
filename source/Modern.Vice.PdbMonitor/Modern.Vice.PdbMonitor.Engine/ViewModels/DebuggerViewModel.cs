using System.IO;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Engine.Models;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class DebuggerViewModel: ScopedViewModel
    {
        readonly ILogger<DebuggerViewModel> logger;
        readonly Globals globals;
        public string? ProjectName => Path.GetFileName(globals.Project?.PrgPath);
        public Project? Project => globals.Project;
        public bool IsOpenProject => Project is not null;
        public ProjectExplorerViewModel ProjectExplorer { get; }
        public SourceFileViewerViewModel SourceFileViewerViewModel { get; }
        public DebuggerViewModel(ILogger<DebuggerViewModel> logger, Globals globals, ProjectExplorerViewModel projectExplorerViewModel,
            SourceFileViewerViewModel sourceFileViewerViewModel)
        {
            this.logger = logger;
            this.globals = globals;
            ProjectExplorer = projectExplorerViewModel;
            SourceFileViewerViewModel = sourceFileViewerViewModel;
            globals.PropertyChanged += Globals_PropertyChanged;
        }

        void Globals_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Globals.Project):
                    OnPropertyChanged(nameof(Project));
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                globals.PropertyChanged -= Globals_PropertyChanged;
            }
            base.Dispose(disposing);
        }
    }
}
