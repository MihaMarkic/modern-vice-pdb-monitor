using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Services.Abstract;
using Righthand.ViceMonitor.Bridge.Services.Implementation;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class DebuggerViewModel : ScopedViewModel
{
    readonly ILogger<DebuggerViewModel> logger;
    readonly Globals globals;
    readonly IDispatcher dispatcher;
    readonly ExecutionStatusViewModel executionStatusViewModel;
    readonly BreakpointsViewModel breakpointsViewModel;
    readonly IProjectFactory projectFactory;
    IPdbManager? pdbManager;
    public IDebugStepper? DebugStepper { get; private set; }
    public RegistersViewModel Registers {get;}
    public string? ProjectName => Path.GetFileName(globals.Project?.PrgPath);
    public Project? Project => globals.Project;
    public bool IsOpenProject => Project is not null;
    public ProjectExplorerViewModel ProjectExplorer { get; }
    public SourceFileViewerViewModel SourceFileViewerViewModel { get; }
    public VariablesViewModel Variables { get; }
    PdbLine? lastActiveLine;
    public DebuggerViewModel(ILogger<DebuggerViewModel> logger, Globals globals, ProjectExplorerViewModel projectExplorerViewModel,
        SourceFileViewerViewModel sourceFileViewerViewModel, RegistersViewModel registers, IDispatcher dispatcher,
        ExecutionStatusViewModel executionStatusViewModel, 
        VariablesViewModel variablesViewModel,
        BreakpointsViewModel breakpointsViewModel,
        IProjectFactory projectFactory)
    {
        this.logger = logger;
        this.globals = globals;
        this.dispatcher = dispatcher;
        this.executionStatusViewModel = executionStatusViewModel;
        this.breakpointsViewModel = breakpointsViewModel;
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
            DebugStepper = projectFactory.GetDebugStepper(globals.Project.CompilerType);
        }
        else
        {
            pdbManager = null;
            DebugStepper = null;
        }
    }

    void ExecutionStatusViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (executionStatusViewModel.IsStartingDebugging)
        {
            return;
        }
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
                if (!executionStatusViewModel.IsDebuggingPaused && !executionStatusViewModel.IsStepping)
                {
                    Variables.CancelUpdateForLine();
                }
                if (executionStatusViewModel.IsDebuggingPaused && registersUpdated)
                {
                    UpdateState();
                    registersUpdated = false;
                }
                break;
        }
    }
    
    internal async Task StepIntoAsync()
    {
        if (DebugStepper is not null)
        {
            await DebugStepper.StepIntoAsync(lastActiveLine);
        }
    }
    internal async Task StepOverAsync()
    {
        if (DebugStepper is not null)
        {
            await DebugStepper.StepOverAsync(lastActiveLine);
        }
    }
    internal async Task ContinueAsync()
    {
        if (DebugStepper is not null)
        {
            await DebugStepper.ContinueAsync(lastActiveLine);
        }
    }
    internal async Task ExitViceMonitorAsync()
    {
        if (DebugStepper is not null)
        {
            await DebugStepper.ExitViceMonitorAsync();
        }
    }

    bool registersUpdated;
    void Registers_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Registers.Current):

                // first exclude processing based on execution status
                if (pdbManager is null
                    || !executionStatusViewModel.IsDebugging || executionStatusViewModel.IsStartingDebugging)
                {
                    return;
                }
                registersUpdated = true;
                break;
        }
    }

    internal void UpdateState()
    {
        ushort? address = Registers.Current.PC;
        if (pdbManager is not null && address.HasValue)
        {
            var matchingLine = pdbManager.FindLineUsingAddress(address.Value);
            lastActiveLine = matchingLine;
            Debug.WriteLine($"Got address {address.Value:X4}");
            if (matchingLine is not null)
            {
                // don't do anything when DebugStepper is active as it takes control
                if (DebugStepper?.IsActive == true)
                {
                    bool isTimeout = DebugStepper.IsTimeout(DateTimeOffset.Now);
                    if (!isTimeout && matchingLine == DebugStepper.StartLine)
                    {
                        DebugStepper.Continue();
                        return;
                    }
                    else
                    {
                        DebugStepper.Stop();
                    }
                }                // when not stepping in/over, stop only on lines that are under a breakpoint
                else
                {
                    if (breakpointsViewModel.GetBreakpointsAssociatedWithLine(matchingLine).IsEmpty)
                    {
                        return;
                    }
                }
                var file = pdbManager.FindFileOfLine(matchingLine)!;
                int matchingLineNumber = file.Lines.IndexOf(matchingLine);
                dispatcher.Dispatch(
                    new OpenSourceFileMessage(file, ExecutingLine: matchingLineNumber)
                );
                _ = Variables.StartUpdateForLineAsync(matchingLine);
                return;
            }
            else if (DebugStepper?.IsActive == true)
            {
                bool isTimeout = DebugStepper.IsTimeout(DateTimeOffset.Now);
                if (isTimeout)
                {
                    logger.LogWarning("Timeout while stepping. Stepping will stop.");
                    DebugStepper.Stop();
                }
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

    public void CloseProject()
    {
        SourceFileViewerViewModel.CloseAll();
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
