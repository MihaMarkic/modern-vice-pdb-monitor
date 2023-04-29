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

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class ProjectExplorerViewModel : NotifiableObject
{
    readonly ILogger<ProjectExplorerViewModel> logger;
    readonly IDispatcher dispatcher;
    readonly Globals globals;
    readonly BreakpointsViewModel breakpoints;
    readonly IProjectFactory projectFactory;
    IPdbManager? pdbManager;
    public string? ProjectName => Path.GetFileName(globals.Project?.PrgPath);
    public Project? Project => globals.Project;
    public ImmutableArray<object> Nodes { get; private set; }
    public RelayCommand<object> OpenSourceFileCommand { get; }
    public RelayCommandAsync<PdbLabel> AddBreakpointOnLabelCommand { get; }
    ImmutableArray<PdbFile> files = ImmutableArray<PdbFile>.Empty;
    public ProjectExplorerViewModel(IDispatcher dispatcher, ILogger<ProjectExplorerViewModel> logger, Globals globals,
        BreakpointsViewModel breakpoints, IProjectFactory projectFactory)
    {
        this.dispatcher = dispatcher;
        this.logger = logger;
        this.globals = globals;
        this.breakpoints = breakpoints;
        this.projectFactory = projectFactory;
        OpenSourceFileCommand = new RelayCommand<object>(OpenSourceFile, canExecute: o => o is PdbFile || o is PdbLabel);
        AddBreakpointOnLabelCommand = new RelayCommandAsync<PdbLabel>(AddBreakpointOnLabelAsync, CanAddBreakpointOnLabel);
        globals.PropertyChanged += Globals_PropertyChanged;
        UpdateNodes();
        UpdatePdbManager();
    }
    void UpdatePdbManager()
    {
        if (globals.Project?.CompilerType is not null)
        {
            pdbManager = projectFactory.GetPdbManager(globals.Project.CompilerType);
        }
        else
        {
            pdbManager = null;
        }
    }

    void Globals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Globals.Project):
                OnPropertyChanged(nameof(Project));
                UpdatePdbManager();
                UpdateNodes();
                break;
            case Globals.ProjectDebugSymbols:
                UpdateNodes();
                break;
        }
    }
    internal bool CanAddBreakpointOnLabel(PdbLabel? label)
    {
        if (pdbManager is null)
        {
            return false;
        }
        if (label is not null)
        {
            var line = pdbManager.FindLineUsingAddress(label.Address);
            return line is not null;
        }
        return false;
    }
    internal async Task AddBreakpointOnLabelAsync(PdbLabel? label)
    {
        if (label is not null)
        {
            await breakpoints.AddBreakpointForLabelAsync(label, condition: null);
        }
    }
    internal void OpenSourceFile(object? item)
    {
        if (pdbManager is null)
        {
            return;
        }
        switch (item)
        {
            case PdbFile pdbFile:
                dispatcher.Dispatch(new OpenSourceFileMessage(pdbFile));
                break;
            case PdbLabel label:
                var line = pdbManager.FindLineUsingAddress(label.Address);
                if (line is not null)
                {
                    var file = pdbManager.FindFileOfLine(line);
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
            files = globals.Project?.DebugSymbols?.Files.Values.ToImmutableArray() ?? ImmutableArray<PdbFile>.Empty;
            Nodes = ImmutableArray<object>.Empty
                .Add(new ProjectExplorerHeaderNode("Files", files))
                .Add(new ProjectExplorerHeaderNode("Labels", globals.Project?.DebugSymbols?.Labels.Values.ToImmutableArray() ?? ImmutableArray<PdbLabel>.Empty));
        }
        else
        {
            Nodes = Nodes.Clear();
            files = ImmutableArray<PdbFile>.Empty;
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
