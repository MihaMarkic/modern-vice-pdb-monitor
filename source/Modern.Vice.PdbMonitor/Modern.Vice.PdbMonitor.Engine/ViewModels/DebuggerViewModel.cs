using Compiler.Oscar64.Models;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Righthand.MessageBus;
using System.ComponentModel;
using System.IO;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class DebuggerViewModel : ScopedViewModel
{
    readonly ILogger<DebuggerViewModel> logger;
    readonly Globals globals;
    readonly IDispatcher dispatcher;
    readonly ExecutionStatusViewModel executionStatusViewModel;
    readonly IProjectFactory projectFactory;
    IPdbManager? pdbManager;
    public RegistersViewModel Registers {get;}
    public string? ProjectName => Path.GetFileName(globals.Project?.PrgPath);
    public Project? Project => globals.Project;
    public bool IsOpenProject => Project is not null;
    public ProjectExplorerViewModel ProjectExplorer { get; }
    public SourceFileViewerViewModel SourceFileViewerViewModel { get; }
    public VariablesViewModel Variables { get; }
    public DebuggerViewModel(ILogger<DebuggerViewModel> logger, Globals globals, ProjectExplorerViewModel projectExplorerViewModel,
        SourceFileViewerViewModel sourceFileViewerViewModel, RegistersViewModel registers, IDispatcher dispatcher,
        ExecutionStatusViewModel executionStatusViewModel, 
        VariablesViewModel variablesViewModel,
        IProjectFactory projectFactory)
    {
        this.logger = logger;
        this.globals = globals;
        this.dispatcher = dispatcher;
        this.executionStatusViewModel = executionStatusViewModel;
        this.projectFactory = projectFactory;
        executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
        ProjectExplorer = projectExplorerViewModel;
        SourceFileViewerViewModel = sourceFileViewerViewModel;
        Variables = variablesViewModel;
        Registers = registers;
        Registers.PropertyChanged += Registers_PropertyChanged;
        globals.PropertyChanged += Globals_PropertyChanged;
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

    void ExecutionStatusViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ExecutionStatusViewModel.IsDebugging):
                // clears execution rows
                if (!executionStatusViewModel.IsDebugging)
                {
                    foreach (var fileViewer in SourceFileViewerViewModel.Files)
                    {
                        fileViewer.ClearExecutionRow();
                    }
                }
                else
                {
                    Variables.CancelUpdateForLine();
                }    
                break;
            case nameof(ExecutionStatusViewModel.IsDebuggingPaused):
                if (!executionStatusViewModel.IsDebuggingPaused)
                {
                    Variables.CancelUpdateForLine();
                }
                break;
        }
    }

    void Registers_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (pdbManager is null)
        {
            return;
        }
        ushort? address = Registers.Current.PC;
        if (address.HasValue)
        {
            var matchingLine = pdbManager.FindLineUsingAddress(address.Value);
            if (matchingLine is not null)
            {
                var file = pdbManager.FindFileOfLine(matchingLine)!;
                int matchingLineNumber = file.Lines.IndexOf(matchingLine);
                dispatcher.Dispatch(
                    new OpenSourceFileMessage(file, ExecutingLine: matchingLineNumber)
                );
                _ = Variables.StartUpdateForLineAsync(matchingLine);
                return;
            }
        }
        Variables.CancelUpdateForLine();
        SourceFileViewerViewModel.ClearExecutionRow();
    }
    
    void Globals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Globals.Project):
                OnPropertyChanged(nameof(Project));
                UpdatePdbManager();
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            executionStatusViewModel.PropertyChanged -= ExecutionStatusViewModel_PropertyChanged;
            Registers.PropertyChanged -= Registers_PropertyChanged;
            globals.PropertyChanged -= Globals_PropertyChanged;
        }
        base.Dispose(disposing);
    }
}
