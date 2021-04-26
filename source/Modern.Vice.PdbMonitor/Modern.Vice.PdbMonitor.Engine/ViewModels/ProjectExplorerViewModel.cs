using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class ProjectExplorerViewModel : NotifiableObject
    {
        readonly ILogger<ProjectExplorerViewModel> logger;
        readonly IDispatcher dispatcher;
        readonly Globals globals;
        public string? ProjectName => Path.GetFileName(globals.Project?.PrgPath);
        public Project? Project => globals.Project;
        public ImmutableArray<ProjectExplorerHeaderNode> Nodes { get; private set; }
        public RelayCommand<object> OpenSourceFileCommand { get; }
        ImmutableDictionary<int, AcmePdbAddress> addresses = ImmutableDictionary<int, AcmePdbAddress>.Empty;
        ImmutableArray<AcmePdbFile> files = ImmutableArray<AcmePdbFile>.Empty;
        public ProjectExplorerViewModel(IDispatcher dispatcher, ILogger<ProjectExplorerViewModel> logger, Globals globals)
        {
            this.dispatcher = dispatcher;
            this.logger = logger;
            this.globals = globals;
            OpenSourceFileCommand = new RelayCommand<object>(OpenSourceFile, canExecute: o => o is AcmePdbFile || o is AcmePdbLabel);
            globals.PropertyChanged += Globals_PropertyChanged;
            UpdateNodes();
        }
        
        void Globals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Globals.Project):
                    OnPropertyChanged(nameof(Project));
                    UpdateNodes();
                    break;
                case nameof(Globals.Pdb):
                    UpdateNodes();
                    break;
            }
        }
        internal void OpenSourceFile(object? message)
        {
            switch (message)
            {
                case AcmePdbFile pdbFile:
                    dispatcher.Dispatch(new OpenSourceFileMessage(pdbFile));
                    break;
                case AcmePdbLabel label:
                    if (addresses.TryGetValue(label.Address, out var address))
                    {
                        dispatcher.Dispatch(new OpenSourceFileMessage(files[address.FileIndex], address.Line));
                    }
                    break;
            }
        }
        internal void UpdateNodes()
        {
            if (Project is not null)
            {
                var addresses = globals.Pdb?.Addresses ?? ImmutableArray<AcmePdbAddress>.Empty;
                files = globals.Pdb?.Files ?? ImmutableArray<AcmePdbFile>.Empty;
                Nodes = ImmutableArray<ProjectExplorerHeaderNode>.Empty
                    .Add(new ProjectExplorerHeaderNode("Files", files))
                    .Add(new ProjectExplorerHeaderNode("Labels", globals.Pdb?.Labels ?? ImmutableArray<AcmePdbLabel>.Empty))
                    .Add(new ProjectExplorerHeaderNode("Includes", globals.Pdb?.Includes ?? ImmutableArray<AcmePdbInclude>.Empty));
                this.addresses = addresses.ToImmutableDictionary(a => a.Address);
            }
            else
            {
                Nodes = Nodes.Clear();
                addresses = ImmutableDictionary<int, AcmePdbAddress>.Empty;
                files = ImmutableArray<AcmePdbFile>.Empty;
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
