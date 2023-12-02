using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using FuzzySharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Models.Configuration;
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
    readonly ISubscription debugDataChangedSubscription;
    public RelayCommandAsync<BreakpointViewModel> ToggleBreakpointEnabledCommand { get; }
    public RelayCommandAsync<BreakpointViewModel> ShowBreakpointPropertiesCommand { get; }
    public RelayCommandAsync<BreakpointViewModel> RemoveBreakpointCommand { get; }
    public RelayCommandAsync RemoveAllBreakpointsCommand { get; }
    public RelayCommandAsync CreateBreakpointCommand { get; }
    public bool IsWorking { get; private set; }
    /// <summary>
    /// When true, it shouldn't update breakpoints settings
    /// </summary>
    bool suppressLocalPersistence;
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
        Breakpoints.CollectionChanged += Breakpoints_CollectionChanged;
        breakpointsLinesMap = new Dictionary<PdbLine, List<BreakpointViewModel>>();
        breakpointsMap = new Dictionary<uint, BreakpointViewModel>();
        ToggleBreakpointEnabledCommand = new RelayCommandAsync<BreakpointViewModel>(ToggleBreakpointEnabledAsync);
        ShowBreakpointPropertiesCommand = new RelayCommandAsync<BreakpointViewModel>(ShowBreakpointPropertiesAsync, b => b is not null);
        RemoveBreakpointCommand = new RelayCommandAsync<BreakpointViewModel>(RemoveBreakpointAsync, b => b is not null);
        // TODO disable breakpoints manipulation when vice is not connected
        RemoveAllBreakpointsCommand = new RelayCommandAsync(RemoveAllBreakpointsAsync);
        CreateBreakpointCommand = new RelayCommandAsync(CreateBreakpoint);
        viceBridge.ViceResponse += ViceBridge_ViceResponse;
        globals.PropertyChanged += Globals_PropertyChanged;
        executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
        debugDataChangedSubscription = dispatcher.Subscribe<DebugDataChangedMessage>(SubscriptionDebugDataChanged);
        UpdatePdbManager();
    }

    void Breakpoints_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        SaveLocalSettings();
    }

    async Task SubscriptionDebugDataChanged(DebugDataChangedMessage message, CancellationToken ct)
    {
        await ApplyOriginalBreakpointsOnNewPdbAsync(CancellationToken.None);
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
                _ = RemoveAllBreakpointsAsync(false);
                UpdatePdbManager();
                break;
            case nameof(Globals.ProjectDebugSymbols):
                if (!executionStatusViewModel.IsOpeningProject)
                {
                    _ = ApplyOriginalBreakpointsOnNewPdbAsync(CancellationToken.None);
                }
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
    void SaveLocalSettings()
    {
        if (!suppressLocalPersistence)
        {
            globals.SaveBreakpoints(Breakpoints);
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
    internal async Task RemoveAllBreakpointsAsync()
    {
        await RemoveAllBreakpointsAsync(removeFromLocalStorage: true);
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
    /// <param name="removeFromLocalStorage">When true, breakpoints are removed from persistence, left otherwise.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <remarks>
    /// <paramref name="removeFromLocalStorage"/> is used when app is cleaning breakpoints but they shouldn't be removed
    /// from persistence.
    /// </remarks>
    internal async Task RemoveAllBreakpointsAsync(bool removeFromLocalStorage, CancellationToken ct = default)
    {
        suppressLocalPersistence = true;
        try
        {
            while (Breakpoints.Count > 0)
            {
                await RemoveBreakpointAsync(Breakpoints[0], true, ct);
            }
        }
        finally
        {
            suppressLocalPersistence = false;
            if (removeFromLocalStorage)
            {
                SaveLocalSettings();
            }
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
    public async Task RearmBreakpoints(bool hasPdbChanged, CancellationToken ct)
    {
        Debug.Assert(Breakpoints.All(bp => bp.CheckpointNumber is null));
        suppressLocalPersistence = true;
        try
        {
            // reapply breakpoints
            if (hasPdbChanged)
            {
                await ApplyOriginalBreakpointsOnNewPdbAsync(ct);
            }
            foreach (var breakpoint in Breakpoints)
            {
                await ArmBreakpointAsync(breakpoint, ct);
            }
        }
        finally
        {
            suppressLocalPersistence = false;
            SaveLocalSettings();
        }
        logger.LogDebug("Checkpoints reapplied");
    }
    public async Task DisarmAllBreakpoints(CancellationToken ct)
    {
        // collects all applied check points in VICE
        var checkpointsListCommand = viceBridge.EnqueueCommand(new CheckpointListCommand(), resumeOnStopped: true);
        var checkpointsList = await checkpointsListCommand.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, checkpointsListCommand, ct: ct);
        if (checkpointsList is not null)
        {
            foreach (var ci in checkpointsList.Info)
            {
                if (breakpointsMap.TryGetValue(ci.CheckpointNumber, out var breakpoint))
                {
                    // deletes only those that are part of breakpoints
                    await DeleteCheckpointAsync(ci.CheckpointNumber, ct);
                    breakpoint.CheckpointNumber = null;
                }
            }
        }
        breakpointsLinesMap.Clear();
        breakpointsMap.Clear();
    }
    /// <summary>
    /// There are three types of breakpoint to reapply: bound to label, line and unbound.
    /// </summary>
    /// <param name="breakpoints"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    internal async Task ApplyOriginalBreakpointsOnNewPdbAsync(CancellationToken ct)
    {
        if (pdbManager is null)
        {
            return;
        }
        bool anyRemoved = false;
        var originalBreakpoints = Breakpoints.ToImmutableArray();
        Breakpoints.Clear();
        foreach (var breakpoint in originalBreakpoints)
        {
            // first reapply breakpoints bound to a label
            if (breakpoint.Label is not null)
            {
                var label = pdbManager.FindLabel(breakpoint.Label.Name);
                if (label is not null)
                {
                    var line = pdbManager.FindLineUsingAddress(breakpoint.Label.Address);
                }
                else
                {
                    logger.Log(LogLevel.Information, "Breakpoint on label {label} not reapplied", breakpoint.Label.Name);
                }
            }
            // then ones bound to a line
            else if (breakpoint.File is not null && breakpoint.Line is not null)
            {
                var criteria = new LineSearchCriteria(breakpoint.Line.LineNumber, breakpoint.Line.Text);
                var match = FindMatchingLine(globals.Project?.DebugSymbols, breakpoint.File, criteria);
                if (match?.Line?.Addresses.IsDefaultOrEmpty == false)
                {
                    await AddBreakpointAsync(match.Value.File, match.Value.Line, match.Value.Line.LineNumber, null, null, ct);
                }
                else
                {
                    logger.Log(LogLevel.Information, "Breakpoint on file  {file} and line {line_number} {line} not reapplied", 
                        breakpoint.File.Path, breakpoint.Line.LineNumber, breakpoint.Line.Text);
                    Breakpoints.Remove(breakpoint);
                    anyRemoved = true;
                }
            }
        }
        if (anyRemoved)
        {
            logger.Log(LogLevel.Information, "Not all {number} breakpoints were reapplied", Breakpoints.Count);
        }
        else
        {
            logger.Log(LogLevel.Information, "All {number} breakpoints were reapplied", Breakpoints.Count);
        }
    }
    public  record LineSearchCriteria(int LineNumber, string Text);
    /// <summary>
    /// Tries to figure out the line to apply breakpoint to
    /// </summary>
    /// <param name="originalLine"></param>
    /// <returns></returns>
    internal static (PdbFile File, PdbLine Line)? FindMatchingLine(Pdb? pdb, PdbFile originalFile, LineSearchCriteria originalLine)
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
    internal static PdbLine? FuzzyFindLine(int maxDelta, int minScoreToMatch, LineSearchCriteria originalLine, ImmutableArray<PdbLine> lines)
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
    internal static ImmutableArray<PdbLine> GetCandidatesForFuzzySearch(int lineIndex, int maxDelta, 
        ImmutableArray<PdbLine> lines)
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
        if (!executionStatusViewModel.IsProcessingDisabled)
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
        if (breakpoint?.CheckpointNumber is not null)
        {
            var command = viceBridge.EnqueueCommand(new CheckpointToggleCommand(breakpoint.CheckpointNumber.Value, 
                !breakpoint.IsEnabled), resumeOnStopped: true);
            var result = await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command);
            if (result is not null)
            {
                breakpoint.IsEnabled = !breakpoint.IsEnabled;
            }
        }
    }
    ///// <summary>
    ///// Wrapper around the other <see cref="AddBreakpointAsync(bool, bool, BreakpointMode, PdbLine?, int?, PdbFile?, 
    ///// PdbLabel?, ushort, ushort, string?, CancellationToken)"/>
    ///// </summary>
    ///// <param name="breakpoint"></param>
    ///// <param name="ct"></param>
    ///// <returns></returns>
    //internal async Task AddBreakpointAsync(BreakpointViewModel breakpoint, CancellationToken ct = default)
    //{
    //    await AddBreakpointAsync(breakpoint.StopWhenHit, breakpoint.IsEnabled, breakpoint.Mode,
    //                breakpoint.Line, breakpoint.LineNumber - 1, breakpoint.File, breakpoint.Label,
    //                ImmutableArray<AddressRange>.Empty.Add(AddressRange.FromRange(breakpoint.StartAddress, breakpoint.EndAddress)), 
    //                breakpoint.Condition, ct);
    //}
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
            if (range.EndAddress < range.StartAddress)
            {
                throw new Exception($"Invalid breakpoint address range {range.StartAddress} to {range.EndAddress}");
            }

            var breakpoint = new BreakpointViewModel(stopWhenHit,
                    isEnabled, mode,
                        line, lineNumber + 1, file, label,
                        range.StartAddress, range.EndAddress, condition);
            await AddBreakpointAsync(breakpoint, ct);
        }
    }
    internal async Task AddBreakpointAsync(BreakpointViewModel breakpoint, CancellationToken ct)
    {
        Breakpoints.Add(breakpoint);
        if (executionStatusViewModel.IsDebugging)
        {
            await ArmBreakpointAsync(breakpoint, ct);
        }
    }
    public async Task ArmBreakpointAsync(BreakpointViewModel breakpoint, CancellationToken ct)
    {
        var checkpointSetCommand = viceBridge.EnqueueCommand(
            new CheckpointSetCommand(breakpoint.StartAddress, breakpoint.EndAddress, breakpoint.StopWhenHit,
                breakpoint.IsEnabled, CpuOperation.Exec, false),
               resumeOnStopped: true);
        var checkpointSetResponse = await checkpointSetCommand.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger,
            checkpointSetCommand, ct: ct);
        if (checkpointSetResponse is not null)
        {
            breakpoint.CheckpointNumber = checkpointSetResponse.CheckpointNumber;
            // apply condition to checkpoint if any
            if (!string.IsNullOrWhiteSpace(breakpoint.Condition))
            {
                var conditionSetCommand = viceBridge.EnqueueCommand(
                    new ConditionSetCommand(checkpointSetResponse.CheckpointNumber, breakpoint.Condition),
                    resumeOnStopped: true);
                var conditionSetResponse = conditionSetCommand.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger,
                    conditionSetCommand, ct: ct);
                // in case condition set fails, remove the checkpoint
                if (conditionSetResponse is null)
                {
                    var checkpointDeleteCommand = viceBridge.EnqueueCommand(
                        new CheckpointDeleteCommand(checkpointSetResponse.CheckpointNumber),
                            resumeOnStopped: true);
                    await checkpointDeleteCommand.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, checkpointDeleteCommand, ct: ct);
                    return;
                }
            }
            if (breakpoint.Line is not null)
            {
                if (!breakpointsLinesMap.TryGetValue(breakpoint.Line, out var breakpoints))
                {
                    breakpoints = new List<BreakpointViewModel> { breakpoint };
                    breakpointsLinesMap.Add(breakpoint.Line, breakpoints);
                }
                else
                {
                    breakpointsLinesMap[breakpoint.Line].Add(breakpoint);
                }
            }
            breakpointsMap.Add(breakpoint.CheckpointNumber.Value, breakpoint);
        }
    }
    public async Task<bool> RemoveBreakpointAsync(BreakpointViewModel breakpoint, bool forceRemove,
        CancellationToken ct = default)
    {
        if (breakpoint.CheckpointNumber is not null)
        {
            bool success = await DeleteCheckpointAsync(breakpoint.CheckpointNumber.Value, ct);
            if (success || forceRemove)
            {
                RemoveBreakpointFromListByCheckpointNumber(breakpoint.CheckpointNumber.Value);
                if (breakpoint.Line is not null)
                {
                    breakpointsLinesMap.Remove(breakpoint.Line);
                }
                breakpointsMap.Remove(breakpoint.CheckpointNumber.Value);
                return true;
            }
            return false;
        }
        else
        {
            return Breakpoints.Remove(breakpoint);
        }
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
        var command = viceBridge.EnqueueCommand(new CheckpointDeleteCommand(checkpointNumber),
            resumeOnStopped: true);
        var result = await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command, ct: ct);
        return result is not null;
    }
    /// <summary>
    /// Updates an existing breakpoint. Will throw if problems with communication or condition fails.
    /// When breakpoint is armed, it will be replaced with new one, nothing is done otherwise.
    /// </summary>
    /// <param name="breakpoint"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <remarks>Breakpoint might be a clone and thus equality on <see cref="BreakpointViewModel"/> can not be used.</remarks>
    public async Task UpdateBreakpointAsync(BreakpointViewModel breakpoint, CancellationToken ct = default)
    {
        if (breakpoint.CheckpointNumber is not null)
        {
            bool result = await DeleteCheckpointAsync(breakpoint.CheckpointNumber.Value, ct);
            if (!result)
            {
                throw new Exception("Failed to remove breakpoint from the list");
            }
            await ArmBreakpointAsync(breakpoint, ct);
        }
    }

    public Task LoadBreakpointsFromSettingsAsync(BreakpointsSettings settings, CancellationToken ct = default)
    {
        if (globals.Project?.DebugSymbols is not null)
        {
            var debug = globals.Project.DebugSymbols;
            var items = new List<BreakpointViewModel>(settings.Breakpoints.Length);
            foreach (var b in settings.Breakpoints)
            {
                BreakpointViewModel? breakpoint = null;
                if (b.FilePath is not null)
                {
                    if (debug.Files.TryGetValue(b.FilePath, out var file) && b.LineNumber is not null && b.Text is not null)
                    {
                        var criteria = new LineSearchCriteria(b.LineNumber.Value, b.Text);
                        var match = FindMatchingLine(globals.Project?.DebugSymbols, file, criteria);
                        if (match is not null)
                        {
                            breakpoint = new BreakpointViewModel
                            {
                                StopWhenHit = b.StopWhenHit,
                                IsEnabled = b.IsEnabled,
                                Mode = b.Mode,
                                StartAddress = b.StartAddress,
                                EndAddress = b.EndAddress,
                                Condition = b.Condition,
                                File = match.Value.File,
                                Line = match.Value.Line,
                                LineNumber = b.LineNumber,
                            };
                        }
                        else
                        {
                            logger.Log(LogLevel.Information,
                                "Breakpoint at line {Line} and with Text {Text} has not been applied because source code is too different",
                                b.LineNumber.Value, b.Text);
                        }
                    }
                }
                else if (b.Label is not null)
                {
                    if (debug.Labels.TryGetValue(b.Label.Name, out var label))
                    {
                        breakpoint = new BreakpointViewModel
                        {
                            StopWhenHit = b.StopWhenHit,
                            IsEnabled = b.IsEnabled,
                            Mode = b.Mode,
                            StartAddress = b.StartAddress,
                            EndAddress = b.EndAddress,
                            Condition = b.Condition,
                            Label = label,
                        };
                    }
                }
                if (breakpoint is not null)
                {
                    Breakpoints.Add(breakpoint);
                }
            }
        }
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            viceBridge.ViceResponse -= ViceBridge_ViceResponse;
            executionStatusViewModel.PropertyChanged -= ExecutionStatusViewModel_PropertyChanged;
            debugDataChangedSubscription.Dispose();
        }
        base.Dispose(disposing);
    }
}
