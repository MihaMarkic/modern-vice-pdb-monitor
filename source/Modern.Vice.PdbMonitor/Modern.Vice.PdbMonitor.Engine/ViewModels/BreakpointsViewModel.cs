using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FuzzySharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class BreakpointsViewModel: NotifiableObject
{
    readonly ILogger<RegistersViewModel> logger;
    readonly IDispatcher dispatcher;
    readonly IViceBridge viceBridge;
    readonly ExecutionStatusViewModel executionStatusViewModel;
    readonly Globals globals;
    readonly IServiceScopeFactory serviceScopeFactory;
    readonly IProjectFactory projectFactory;
    public ObservableCollection<BreakpointViewModel> Breakpoints { get; }
    readonly Dictionary<PdbLine, List<BreakpointViewModel>> breakpointsLinesMap;
    readonly Dictionary<uint, BreakpointViewModel> breakpointsMap;
    IPdbManager? pdbManager;
    readonly TaskFactory uiFactory;
    public RelayCommandAsync<BreakpointViewModel> ToggleBreakpointEnabledCommand { get; }
    public RelayCommandAsync<BreakpointViewModel> ShowBreakpointPropertiesCommand { get; }
    public RelayCommandAsync<BreakpointViewModel> RemoveBreakpointCommand { get; }
    public RelayCommandAsync CreateBreakpointCommand { get; }
    public bool IsWorking { get; private set; }
    public BreakpointsViewModel(ILogger<RegistersViewModel> logger, IViceBridge viceBridge, IDispatcher dispatcher, Globals globals,
        ExecutionStatusViewModel executionStatusViewModel, IServiceScopeFactory serviceScopeFactory,
        IProjectFactory projectFactory)
    {
        this.logger = logger;
        this.viceBridge = viceBridge;
        this.dispatcher = dispatcher;
        this.globals = globals;
        this.serviceScopeFactory = serviceScopeFactory;
        this.executionStatusViewModel = executionStatusViewModel;
        this.projectFactory = projectFactory;
        uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        Breakpoints = new ObservableCollection<BreakpointViewModel>();
        breakpointsLinesMap = new Dictionary<PdbLine, List<BreakpointViewModel>>();
        breakpointsMap = new Dictionary<uint, BreakpointViewModel>();
        ToggleBreakpointEnabledCommand = new RelayCommandAsync<BreakpointViewModel>(ToggleBreakpointEnabledAsync);
        ShowBreakpointPropertiesCommand = new RelayCommandAsync<BreakpointViewModel>(ShowBreakpointPropertiesAsync, b => b is not null);
        RemoveBreakpointCommand = new RelayCommandAsync<BreakpointViewModel>(RemoveBreakpointAsync, b => b is not null);
        CreateBreakpointCommand = new RelayCommandAsync(CreateBreakpoint);
        viceBridge.ViceResponse += ViceBridge_ViceResponse;
        globals.PropertyChanged += Globals_PropertyChanged;
        executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
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
    public ImmutableArray<BreakpointViewModel> GetBreakpointsAssociatedWithLine(PdbLine line)
    {
        if (breakpointsLinesMap.TryGetValue(line, out var breakpoints))
        {
            return breakpoints.ToImmutableArray();
        }
        return ImmutableArray<BreakpointViewModel>.Empty;
    }
    void Globals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Globals.Project):
                _ = RemoveAllBreakpointsAsync();
                UpdatePdbManager();
                break;
        }
    }

    internal async Task CreateBreakpoint()
    {
        using (var scope = serviceScopeFactory.CreateScope())
        {
            var detailViewModel = scope.CreateScopedBreakpointDetailViewModel(new BreakpointViewModel(), BreakpointDetailDialogMode.Create);
            var message =
                new ShowModalDialogMessage<BreakpointDetailViewModel, SimpleDialogResult>("Breakpoint properties", DialogButton.OK | DialogButton.Cancel, detailViewModel);
            dispatcher.Dispatch(message);
            var result = await message.Result;
        }
    }
    internal async Task RemoveBreakpointAsync(BreakpointViewModel? breakpoint)
    {
        if (breakpoint is not null)
        {
            try
            {
                await RemoveBreakpointAsync(breakpoint, forceRemove: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove breakpoint");
            }
        }
    }
    internal async Task ShowBreakpointPropertiesAsync(BreakpointViewModel? breakpoint)
    {
        if (breakpoint is not null)
        {
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var detailViewModel = scope.CreateScopedBreakpointDetailViewModel(breakpoint, BreakpointDetailDialogMode.Update);
                var message = 
                    new ShowModalDialogMessage<BreakpointDetailViewModel, SimpleDialogResult>("Breakpoint properties", DialogButton.OK | DialogButton.Cancel, detailViewModel);
                dispatcher.Dispatch(message);
                var result = await message.Result;
            }
        }
    }
    /// <summary>
    /// Removes all breakpoints from VICE and locally
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    internal async Task RemoveAllBreakpointsAsync(CancellationToken ct = default)
    {
        while (Breakpoints.Count > 0)
        {
            await RemoveBreakpointAsync(Breakpoints[0], true, ct);
        }
    }
    void ExecutionStatusViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ExecutionStatusViewModel.IsDebugging) when !executionStatusViewModel.IsDebugging:
            case nameof(ExecutionStatusViewModel.IsDebuggingPaused) when !executionStatusViewModel.IsDebuggingPaused:
                ClearHitBreakpoint();
                break;
        }
    }
    internal void ClearHitBreakpoint()
    {
        foreach (var breakpoint in Breakpoints)
        {
            breakpoint.IsCurrentlyHit = false;
        }
    }
    /// <summary>
    /// Removes all breakpoints and reapplies them again.
    /// </summary>
    /// <param name="hasPdbChanged"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <remarks>This method is required when lost contact with VICE or when debugging symbols change.</remarks>
    public async Task ReapplyBreakpoints(bool hasPdbChanged, CancellationToken ct)
    {
        var checkpointsListCommand = viceBridge.EnqueueCommand(new CheckpointListCommand());
        var checkpointsList = await checkpointsListCommand.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, checkpointsListCommand, ct: ct);
        if (checkpointsList is not null)
        {
            foreach (var ci in checkpointsList.Info)
            {
                // TODO verify result
                await DeleteCheckpointAsync(ci.CheckpointNumber, ct);
            }
        }
        var breakpoints = Breakpoints.ToImmutableArray();
        Breakpoints.Clear();
        breakpointsLinesMap.Clear();
        breakpointsMap.Clear();
        await ApplyOriginalBreakpointsAsync(breakpoints, hasPdbChanged, ct);
        logger.LogDebug("Checkpoints reapplied");
    }
    internal async Task ApplyOriginalBreakpointsAsync(ImmutableArray<BreakpointViewModel> breakpoints, bool hasPdbChanged, CancellationToken ct)
    {
        // reapply breakpoints
        if (hasPdbChanged)
        {
            await ApplyOriginalBreakpointsOnNewPdbAsync(breakpoints, ct);
        }
        // when PDB has not changed, just simply reapply without checking mismatches
        else
        {
            foreach (var breakpoint in breakpoints)
            {
                await AddBreakpointAsync(breakpoint, ct);
            }
        }
    }
    /// <summary>
    /// There are three types of breakpoint to reapply: bound to label, line and unbound.
    /// </summary>
    /// <param name="breakpoints"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    internal async Task ApplyOriginalBreakpointsOnNewPdbAsync(ImmutableArray<BreakpointViewModel> breakpoints, CancellationToken ct)
    {
        if (pdbManager is null)
        {
            return;
        }
        var newBreakpoints = new List<BreakpointViewModel>(breakpoints.Length);
        foreach (var breakpoint in breakpoints)
        {
            bool isBreakpointReapplied = false;
            // first reapply breakpoints bound to a label
            if (breakpoint.Label is not null)
            {
                var label = pdbManager.FindLabel(breakpoint.Label.Name);
                if (label is not null)
                {
                    var line = pdbManager.FindLineUsingAddress(breakpoint.Label.Address);
                    isBreakpointReapplied = true;
                }
                else
                {
                    logger.Log(LogLevel.Information, "Breakpoint on label {label} not reapplied", breakpoint.Label.Name);
                }
            }
            // then ones bound to a line
            else if (breakpoint.File is not null && breakpoint.Line is not null)
            {
                var match = FindMatchingLine(globals.Project?.DebugSymbols, breakpoint.File, breakpoint.Line);
                if (match is not null)
                {
                    breakpoint.Line = match.Value.Line;
                    breakpoint.File = match.Value.File;
                    newBreakpoints.Add(breakpoint);
                    isBreakpointReapplied = true;
                }
                else
                {
                    logger.Log(LogLevel.Information, "Breakpoint on file  {file} and line {line_number} {line} not reapplied", 
                        breakpoint.File.Path, breakpoint.Line.LineNumber, breakpoint.Line.Text);
                }
            }
            // for now set all non-line breakpoints as well
            else
            {
                newBreakpoints.Add(breakpoint);
                isBreakpointReapplied = true;
            }
            if (isBreakpointReapplied)
            {
                await AddBreakpointAsync(breakpoint, ct);
            }
        }
        if (newBreakpoints.Count == breakpoints.Length)
        {
            logger.Log(LogLevel.Information, "All {number} breakpoints were reapplied", breakpoints.Length);
        }
    }
    /// <summary>
    /// Tries to figure out the line to apply breakpoint to
    /// </summary>
    /// <param name="originalLine"></param>
    /// <returns></returns>
    internal static (PdbFile File, PdbLine Line)? FindMatchingLine(Pdb? pdb, PdbFile originalFile, PdbLine originalLine)
    {
        const int maxDelta = 5;
        const int minScoreToMatch = 90;
        if (pdb is null)
        {
            return null;
        }
        if (!pdb.Files.TryGetValue(originalFile.Path, out var newFile))
        {
            return null;
        }
        int lineIndex = originalLine.LineNumber - 1;
        if (newFile.Lines.Length > lineIndex && string.Equals(newFile.Lines[lineIndex].Text, originalLine.Text, StringComparison.Ordinal))
        {
            return (newFile, newFile.Lines[lineIndex]);
        }
        else
        {
            var newLine = FuzzyFindLine(maxDelta, minScoreToMatch, originalLine, newFile.Lines);
            if (newLine is not null)
            {
                return (newFile, newLine);
            }
        }
        return default;
    }
    /// <summary>
    /// Tries to match the line when the exact original one is gone.
    /// </summary>
    /// <param name="maxDelta"></param>
    /// <param name="minScoreToMatch"></param>
    /// <param name="originalLine"></param>
    /// <param name="lines"></param>
    /// <returns></returns>
    internal static PdbLine? FuzzyFindLine(int maxDelta, int minScoreToMatch, PdbLine originalLine, ImmutableArray<PdbLine> lines)
    {
        int lineIndex = originalLine.LineNumber - 1;
        var candidates = GetCandidatesForFuzzySearch(lineIndex, maxDelta, lines);
        // first try exact match
        var match = candidates.Where(l => string.Equals(originalLine.Text, l.Text, StringComparison.Ordinal)).FirstOrDefault();
        if (match is not null)
        {
            return match;
        }
        // adds target line since it can't be fuzzy match
        var weightedCandidates = candidates.Add(lines[lineIndex])
                    .Select(l => new { Score = GetFuzzyMatchScore(originalLine.Text, l.Text), Line = l })
                    .OrderByDescending(p => p.Score)
                    .ToImmutableArray();
        var bestScore = weightedCandidates.First();
        if (bestScore.Score >= minScoreToMatch)
        {
            match = bestScore.Line;
        }
        return match;
    }
    /// <summary>
    /// Get lines that should be examined whether they match to source line. Using +/- one by one.
    /// </summary>
    /// <param name="lineIndex"></param>
    /// <param name="maxDelta"></param>
    /// <param name="lines"></param>
    /// <returns></returns>
    internal static ImmutableArray<PdbLine> GetCandidatesForFuzzySearch(int lineIndex, int maxDelta, ImmutableArray<PdbLine> lines)
    {
        int minBounds = Math.Max(0, lineIndex - maxDelta);
        int maxBounds = Math.Min(lines.Length - 1, lineIndex + maxDelta);
        int maxSteps = Math.Max(lineIndex - minBounds, maxBounds - lineIndex);
        var result = new List<PdbLine>(maxSteps);
        for (int i = 1; i <= maxSteps; i++)
        {
            int proposedIndex = lineIndex + i;
            if (proposedIndex < lines.Length)
            {
                result.Add(lines[proposedIndex]);
            }
            proposedIndex = lineIndex - i;
            if (proposedIndex >= 0)
            {
                result.Add(lines[proposedIndex]);
            }
        }
        return result.ToImmutableArray();
    }
    internal static int GetFuzzyMatchScore(string source, string proposed) => Fuzz.WeightedRatio(source, proposed);
    void ViceBridge_ViceResponse(object? sender, Righthand.ViceMonitor.Bridge.ViceResponseEventArgs e)
    {
        switch (e.Response)
        {
            case CheckpointInfoResponse checkpointInfo:
                uiFactory.StartNew(() =>
                {
                    UpdateBreakpointDataFromVice(checkpointInfo);
                });
                break;
        }
    }

    void UpdateBreakpointDataFromVice(CheckpointInfoResponse checkpointInfo)
    {
        if (breakpointsMap.TryGetValue(checkpointInfo.CheckpointNumber, out var breakpoint))
        {
            breakpoint.IsCurrentlyHit = checkpointInfo.CurrentlyHit;
            breakpoint.IsEnabled = checkpointInfo.Enabled;
            breakpoint.HitCount = checkpointInfo.HitCount;
            breakpoint.IgnoreCount = checkpointInfo.IgnoreCount;
        }
    }
    public async Task AddBreakpointForLabelAsync(PdbLabel label, string? condition, CancellationToken ct = default)
    {
        if (pdbManager is null)
        {
            return;
        }
        // doesn't make sense that there is no line for given label's address
        var line = pdbManager.FindLineUsingAddress(label.Address)!;
        if (!line.Addresses.IsEmpty)
        {
            if (!breakpointsLinesMap.TryGetValue(line, out var breakpoints))
            {
                var file = pdbManager.FindFileOfLine(line)!;
                int lineNumber = file.Lines.IndexOf(line);
                await AddBreakpointAsync(true, true, BreakpointMode.Exec, line, lineNumber, file, label, line.Addresses, null);
            }
            // in case breakpoint at that line already exists, just update it's Label property
            else
            {
                // TODO Update required
                //breakpoints.Label = label;
            }
        }
    }
    /// <summary>
    /// Adds breakpoint linked to line in the file.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="line"></param>
    /// <param name="lineNumber"></param>
    /// <param name="label"></param>
    /// <param name="condition"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task AddBreakpointAsync(PdbFile file, PdbLine line, int lineNumber, PdbLabel? label, string? condition, CancellationToken ct = default)
    {
        if (!line.Addresses.IsEmpty)
        {
            if (!breakpointsLinesMap.TryGetValue(line, out var breakpoint))
            {
                await AddBreakpointAsync(true, true, BreakpointMode.Exec, line, lineNumber, file, label, line.Addresses, null);
            }
        }
    }
    internal async Task ToggleBreakpointEnabledAsync(BreakpointViewModel? breakpoint)
    {
        if (breakpoint is not null)
        {
            var command = viceBridge.EnqueueCommand(new CheckpointToggleCommand(breakpoint.CheckpointNumber, !breakpoint.IsEnabled));
            var result = await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command);
            if (result is not null)
            {
                breakpoint.IsEnabled = !breakpoint.IsEnabled;
            }
        }
    }
    /// <summary>
    /// Wrapper around the other <see cref="AddBreakpointAsync(bool, bool, BreakpointMode, PdbLine?, int?, PdbFile?, 
    /// PdbLabel?, ushort, ushort, string?, CancellationToken)"/>
    /// </summary>
    /// <param name="breakpoint"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    internal async Task AddBreakpointAsync(BreakpointViewModel breakpoint, CancellationToken ct = default)
    {
        await AddBreakpointAsync(breakpoint.StopWhenHit, breakpoint.IsEnabled, breakpoint.Mode,
                    breakpoint.Line, breakpoint.LineNumber - 1, breakpoint.File, breakpoint.Label,
                    ImmutableArray<AddressRange>.Empty.Add(AddressRange.FromRange(breakpoint.StartAddress, breakpoint.EndAddress)), 
                    breakpoint.Condition, ct);
    }
    /// <summary>
    /// Adds breakpoint to VICE
    /// </summary>
    /// <param name="stopWhenHit"></param>
    /// <param name="isEnabled"></param>
    /// <param name="line"></param>
    /// <param name="file"></param>
    /// <param name="startAddress"></param>
    /// <param name="endAddress"></param>
    /// <param name="condition"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    internal async Task AddBreakpointAsync(bool stopWhenHit, bool isEnabled, BreakpointMode mode,
        PdbLine? line, int? lineNumber, PdbFile? file, PdbLabel? label, ImmutableArray<AddressRange> addressRange, string? condition, 
        CancellationToken ct = default)
    {
        foreach (var range in addressRange)
        {
            var checkpointSetCommand = viceBridge.EnqueueCommand(
                       new CheckpointSetCommand(range.StartAddress, range.EndAddress, stopWhenHit, isEnabled, CpuOperation.Exec, false));
            var checkpointSetResponse = await checkpointSetCommand.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, checkpointSetCommand, ct: ct);
            if (checkpointSetResponse is not null)
            {
                // apply condition to checkpoint if any
                if (!string.IsNullOrWhiteSpace(condition))
                {
                    var conditionSetCommand = viceBridge.EnqueueCommand(new ConditionSetCommand(checkpointSetResponse.CheckpointNumber, condition));
                    var conditionSetResponse = conditionSetCommand.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, conditionSetCommand, ct: ct);
                    // in case condition set fails, remove the checkpoint
                    if (conditionSetResponse is null)
                    {
                        var checkpointDeleteCommand = viceBridge.EnqueueCommand(new CheckpointDeleteCommand(checkpointSetResponse.CheckpointNumber));
                        await checkpointDeleteCommand.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, checkpointDeleteCommand, ct: ct);
                        return;
                    }
                }
                var breakpoint = new BreakpointViewModel(checkpointSetResponse.CheckpointNumber, checkpointSetResponse.StopWhenHit,
                        checkpointSetResponse.Enabled, mode,
                            line, lineNumber + 1, file, label,
                            checkpointSetResponse.StartAddress, checkpointSetResponse.EndAddress, condition);
                Breakpoints.Add(breakpoint);
                if (line is not null)
                {
                    if (!breakpointsLinesMap.TryGetValue(line, out var breakpoints))
                    {
                        breakpoints = new List<BreakpointViewModel> { breakpoint };
                        breakpointsLinesMap.Add(line, breakpoints);
                    }
                    else
                    {
                        breakpointsLinesMap[line].Add(breakpoint);
                    }
                }
                breakpointsMap.Add(breakpoint.CheckpointNumber, breakpoint);
            }
        }
    }
    public async Task<bool> RemoveBreakpointAsync(BreakpointViewModel breakpoint, bool forceRemove, CancellationToken ct = default)
    {
        bool success = await DeleteCheckpointAsync(breakpoint.CheckpointNumber, ct);
        if (success || forceRemove)
        {
            RemoveBreakpointFromListByCheckpointNumber(breakpoint.CheckpointNumber);
            if (breakpoint.Line is not null)
            {
                breakpointsLinesMap.Remove(breakpoint.Line);
            }
            breakpointsMap.Remove(breakpoint.CheckpointNumber);
            return true;
        }
        return false;
    }
    bool RemoveBreakpointFromListByCheckpointNumber(uint checkpointNumber)
    {
        var target = Breakpoints.FirstOrDefault(b => b.CheckpointNumber == checkpointNumber);
        if (target is not null)
        {
            return Breakpoints.Remove(target);
        }
        return false;
    }
    /// <summary>
    /// Deletes a checkpoint identified by its checkpoint number on VICE
    /// </summary>
    /// <param name="checkpointNumber"></param>
    /// <param name="ct"></param>
    /// <returns>True when checkpoint was deleted, false otherwise.</returns>
    internal async Task<bool> DeleteCheckpointAsync(uint checkpointNumber, CancellationToken ct)
    {
        var command = viceBridge.EnqueueCommand(new CheckpointDeleteCommand(checkpointNumber));
        var result = await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command, ct: ct);
        return result is not null;
    }
    /// <summary>
    /// Updates an existing breakpoint. Will throw if problems with communication or condition fails.
    /// </summary>
    /// <param name="breakpoint"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <remarks>Breakpoint might be a clone and thus equality on <see cref="BreakpointViewModel"/> can not be used.</remarks>
    public async Task UpdateBreakpointAsync(BreakpointViewModel breakpoint, CancellationToken ct = default)
    {
        bool result = await RemoveBreakpointAsync(breakpoint, forceRemove: false, ct);
        if (!result)
        {
            throw new Exception("Failed to remove breakpoint from the list");
        }
        await AddBreakpointAsync(breakpoint);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            viceBridge.ViceResponse -= ViceBridge_ViceResponse;
            executionStatusViewModel.PropertyChanged -= ExecutionStatusViewModel_PropertyChanged;
        }
        base.Dispose(disposing);
    }
}
