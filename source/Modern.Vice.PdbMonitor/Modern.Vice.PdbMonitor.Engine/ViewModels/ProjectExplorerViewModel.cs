using System.Collections;
using System.Collections.Immutable;
using System.IO;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine.Models;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class ProjectExplorerViewModel : NotifiableObject
    {
        readonly ILogger<ProjectExplorerViewModel> logger;
        readonly Globals globals;
        public string? ProjectName => Path.GetFileName(globals.Project?.PrgPath);
        public Project? Project => globals.Project;
        public ImmutableArray<ProjectExplorerHeaderNode> Nodes { get; private set; }
        public ProjectExplorerViewModel(ILogger<ProjectExplorerViewModel> logger, Globals globals)
        {
            this.logger = logger;
            this.globals = globals;
            globals.PropertyChanged += Globals_PropertyChanged;
            UpdateNodes();
        }

        void Globals_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Globals.Project):
                    OnPropertyChanged(nameof(Project));
                    UpdateNodes();
                    break;
            }
        }

        internal void UpdateNodes()
        {
            if (Project is not null)
            {
                Nodes = ImmutableArray<ProjectExplorerHeaderNode>.Empty
                    .Add(new ProjectExplorerHeaderNode("Files", globals.Pdb?.Files ?? ImmutableArray<AcmePdbFile>.Empty))
                    .Add(new ProjectExplorerHeaderNode("Labels", globals.Pdb?.Labels ?? ImmutableArray<AcmePdbLabel>.Empty))
                    .Add(new ProjectExplorerHeaderNode("Includes", globals.Pdb?.Includes ?? ImmutableArray<AcmePdbInclude>.Empty));
            }
            else
            {
                Nodes = Nodes.Clear();
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

    public record ProjectExplorerHeaderNode(string Name, IList Items);
}
