﻿using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public enum DebuggerStepMode
{
    High,
    Assembly,
}
public class DebuggerViewModel : ScopedViewModel
{
    readonly ILogger<DebuggerViewModel> logger;
    readonly Globals globals;
    readonly IDispatcher dispatcher;
    readonly ExecutionStatusViewModel executionStatusViewModel;
    readonly BreakpointsViewModel breakpointsViewModel;
    readonly EmulatorMemoryViewModel emulatorMemoryViewModel;
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
    public WatchedVariablesViewModel WatchedVariables { get; }
    DebuggerStepMode? stepMode;
    PdbLine? lastActiveLine;
    PdbAssemblyLine? lastActiveAssemblyLine;
    // previous address for update
    ushort? previousAddress;
    public DebuggerViewModel(ILogger<DebuggerViewModel> logger, Globals globals, ProjectExplorerViewModel projectExplorerViewModel,
        SourceFileViewerViewModel sourceFileViewerViewModel, RegistersViewModel registers, IDispatcher dispatcher,
        ExecutionStatusViewModel executionStatusViewModel, 
        VariablesViewModel variablesViewModel,
        WatchedVariablesViewModel watchedVariablesViewModel,
        BreakpointsViewModel breakpointsViewModel,
        EmulatorMemoryViewModel emulatorMemoryViewModel,
        IProjectFactory projectFactory)
    {
        this.logger = logger;
        this.globals = globals;
        this.dispatcher = dispatcher;
        this.executionStatusViewModel = executionStatusViewModel;
        this.breakpointsViewModel = breakpointsViewModel;
        this.emulatorMemoryViewModel = emulatorMemoryViewModel;
        this.projectFactory = projectFactory;
        executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
        ProjectExplorer = projectExplorerViewModel;
        SourceFileViewerViewModel = sourceFileViewerViewModel;
        Variables = variablesViewModel;
        WatchedVariables = watchedVariablesViewModel;
        Registers = registers;
        //Registers.PropertyChanged += Registers_PropertyChanged;
        Registers.RegistersUpdated += Registers_RegistersUpdated;
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
                    Variables.ClearVariables();
                    WatchedVariables.ClearValues();
                }    
                break;
            case nameof(ExecutionStatusViewModel.IsDebuggingPaused):
                if (!executionStatusViewModel.IsDebuggingPaused && !executionStatusViewModel.IsStepping)
                {
                    Variables.ClearVariables();
                    WatchedVariables.ClearValues();
                }
                if (executionStatusViewModel.IsDebuggingPaused && registersUpdated)
                {
                    _ = UpdateStateAsync(CancellationToken.None);
                    registersUpdated = false;
                }
                break;
        }
    }
    
    internal async Task StepIntoAsync(bool isAssemblyStepMode)
    {
        if (DebugStepper is not null)
        {
            stepMode = isAssemblyStepMode ? DebuggerStepMode.Assembly : DebuggerStepMode.High;
            try
            {
                await DebugStepper.StepIntoAsync(lastActiveLine);
            }
            finally
            {
                stepMode = null;
            }
        }
    }
    internal async Task StepOverAsync(bool isAssemblyStepMode)
    {
        if (DebugStepper is not null)
        {
            stepMode = isAssemblyStepMode ? DebuggerStepMode.Assembly : DebuggerStepMode.High;
            try
            {
                await DebugStepper.StepOverAsync(lastActiveLine);
            }
            finally
            {
                stepMode = null;
            }
        }
    }
    internal async Task ContinueAsync()
    {
        if (DebugStepper is not null)
        {
            stepMode = DebuggerStepMode.High;
            try
            {
                await DebugStepper.ContinueAsync(lastActiveLine);
            }
            finally
            {
                stepMode = null;
            }
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
    //void Registers_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    //{
    //    switch (e.PropertyName)
    //    {
    //        case nameof(Registers.Current):

    //            // first exclude processing based on execution status
    //            if (pdbManager is null
    //                || !executionStatusViewModel.IsDebugging || executionStatusViewModel.IsStartingDebugging)
    //            {
    //                return;
    //            }
    //            registersUpdated = true;
    //            break;
    //    }
    //}

    private void Registers_RegistersUpdated(object? sender, EventArgs e)
    {
        // first exclude processing based on execution status
        if (pdbManager is null
            || !executionStatusViewModel.IsDebugging || executionStatusViewModel.IsStartingDebugging)
        {
            return;
        }
        registersUpdated = true;
    }

    internal async Task HandleMatchingLineAsync(PdbLine matchingLine, ushort address, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pdbManager);
        lastActiveAssemblyLine = matchingLine.GetAssemblyLineAtAddress(address);
        // don't do anything when DebugStepper is active as it takes control
        if (DebugStepper?.IsActive == true)
        {
            bool isTimeout = DebugStepper.IsTimeout(DateTimeOffset.Now);
            bool shouldContinue = stepMode switch
            {
                DebuggerStepMode.High => matchingLine == DebugStepper.StartLine,
                DebuggerStepMode.Assembly => address == previousAddress,
                _ => false,
            };
            if (!isTimeout && shouldContinue)
            {
                DebugStepper.Continue();
                return;
            }
            else
            {
                DebugStepper.Stop();
            }
        }
        // when not stepping in/over, stop only on lines that are under a breakpoint
        else
        {
            if (breakpointsViewModel.GetBreakpointsAssociatedWithLine(matchingLine).IsEmpty)
            {
                return;
            }
        }
        var file = pdbManager.FindFileOfLine(matchingLine)!;
        dispatcher.Dispatch(new OpenSourceLineFileMessage(file, matchingLine, lastActiveAssemblyLine, true));
        await emulatorMemoryViewModel.GetSnapshotAsync(ct);
        Variables.UpdateForLine(matchingLine);
        WatchedVariables.UpdateValues();
        return;
    }

    internal async Task UpdateStateAsync(CancellationToken ct)
    {
        ushort? address = Registers.Current.PC;
        try
        {
            if (pdbManager is not null && address.HasValue)
            {
                var matchingLine = pdbManager.FindLineUsingAddress(address.Value);
                lastActiveLine = matchingLine;
                Debug.WriteLine($"Got address {address.Value:X4}");
                if (matchingLine is not null)
                {
                    await HandleMatchingLineAsync(matchingLine, address.Value, ct);
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
        }
        finally
        {
            previousAddress = address;
        }
        Variables.ClearVariables();
        WatchedVariables.ClearValues();
        SourceFileViewerViewModel.ClearExecutionRow();
    }

    public void Clean()
    {
        DebugStepper?.Clean();
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
            Registers.RegistersUpdated -= Registers_RegistersUpdated;
            //Registers.PropertyChanged -= Registers_PropertyChanged;
            globals.PropertyChanged -= Globals_PropertyChanged;
        }
        base.Dispose(disposing);
    }
}
