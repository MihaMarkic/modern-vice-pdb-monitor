using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class ProjectExplorerViewModel : NotifiableObject
    {
        readonly ILogger<ProjectExplorerViewModel> logger;
        readonly IDispatcher dispatcher;
        readonly Globals globals;
        readonly IAcmePdbManager acmePdbManager;
        readonly BreakpointsViewModel breakpoints;
        public string? ProjectName => Path.GetFileName(globals.Project?.PrgPath);
        public Project? Project => globals.Project;
        public ImmutableArray<ProjectExplorerHeaderNode> Nodes { get; private set; }
        public RelayCommand<object> OpenSourceFileCommand { get; }
        public RelayCommandAsync<AcmeLabel> AddBreakpointOnLabelCommand { get; }
        ImmutableArray<AcmeFile> files = ImmutableArray<AcmeFile>.Empty;
        public ProjectExplorerViewModel(IDispatcher dispatcher, ILogger<ProjectExplorerViewModel> logger, Globals globals, IAcmePdbManager acmePdbManager,
            BreakpointsViewModel breakpoints)
        {
            this.dispatcher = dispatcher;
            this.logger = logger;
            this.globals = globals;
            this.acmePdbManager = acmePdbManager;
            this.breakpoints = breakpoints;
            OpenSourceFileCommand = new RelayCommand<object>(OpenSourceFile, canExecute: o => o is AcmeFile || o is AcmeLabel);
            AddBreakpointOnLabelCommand = new RelayCommandAsync<AcmeLabel>(AddBreakpointOnLabelAsync, CanAddBreakpointOnLabel);
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
        internal bool CanAddBreakpointOnLabel(AcmeLabel? label)
{
            if (label is not null)
            {
                var line = acmePdbManager.FindLineUsingAddress(label.Address);
                return line is not null;
            }
            return false;
        }
        internal async Task AddBreakpointOnLabelAsync(AcmeLabel? label)
        {
            if (label is not null)
            {
                await breakpoints.AddBreakpointForLabelAsync(label, condition: null);
            }
        }
        internal void OpenSourceFile(object? item)
        {
            switch (item)
            {
                case AcmeFile acmeFile:
                    dispatcher.Dispatch(new OpenSourceFileMessage(acmeFile));
                    break;
                case AcmeLabel label:
                    var line = acmePdbManager.FindLineUsingAddress(label.Address);
                    if (line is not null)
                    {
                        var file = acmePdbManager.FindFileOfLine(line);
                        // file can't be null actually
                        if (file is not null)
                        {
                            int lineNumber = file.Lines.IndexOf(line);
                            dispatcher.Dispatch(new OpenSourceFileMessage(file, lineNumber, null));
                        }
                    }
                    break;
            }
        }
        internal void UpdateNodes()
        {
            if (Project is not null)
            {
                //var addresses = globals.Pdb?.Addresses ?? ImmutableArray<AcmePdbAddress>.Empty;
                files = globals.Pdb?.Files.Values.ToImmutableArray() ?? ImmutableArray<AcmeFile>.Empty;
                Nodes = ImmutableArray<ProjectExplorerHeaderNode>.Empty
                    .Add(new ProjectExplorerHeaderNode("Files", files))
                    .Add(new ProjectExplorerHeaderNode("Labels", globals.Pdb?.Labels.Values.ToImmutableArray() ?? ImmutableArray<AcmeLabel>.Empty));
            }
            else
            {
                Nodes = Nodes.Clear();
                files = ImmutableArray<AcmeFile>.Empty;
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
