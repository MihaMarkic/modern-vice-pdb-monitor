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

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class BreakpointsViewModel: NotifiableObject
    {
        readonly ILogger<RegistersViewModel> logger;
        readonly IDispatcher dispatcher;
        readonly IViceBridge viceBridge;
        readonly ExecutionStatusViewModel executionStatusViewModel;
        readonly Globals globals;
        readonly IAcmePdbManager acmePdbManager;
        readonly IServiceScopeFactory serviceScopeFactory;
        public ObservableCollection<BreakpointViewModel> Breakpoints { get; }
        readonly Dictionary<AcmeLine, BreakpointViewModel> breakpointsLinesMap;
        readonly Dictionary<uint, BreakpointViewModel> breakpointsMap;
        public RelayCommandAsync<BreakpointViewModel> ToggleBreakpointEnabledCommand { get; }
        public RelayCommandAsync<BreakpointViewModel> ShowBreakpointPropertiesCommand { get; }
        public bool IsWorking { get; private set; }
        public BreakpointsViewModel(ILogger<RegistersViewModel> logger, IViceBridge viceBridge, IDispatcher dispatcher, Globals globals,
            IAcmePdbManager acmePdbManager, ExecutionStatusViewModel executionStatusViewModel, IServiceScopeFactory serviceScopeFactory)
        {
            this.logger = logger;
            this.viceBridge = viceBridge;
            this.dispatcher = dispatcher;
            this.globals = globals;
            this.acmePdbManager = acmePdbManager;
            this.serviceScopeFactory = serviceScopeFactory;
            this.executionStatusViewModel = executionStatusViewModel;
            Breakpoints = new ObservableCollection<BreakpointViewModel>();
            breakpointsLinesMap = new Dictionary<AcmeLine, BreakpointViewModel>();
            breakpointsMap = new Dictionary<uint, BreakpointViewModel>();
            ToggleBreakpointEnabledCommand = new RelayCommandAsync<BreakpointViewModel>(ToggleBreakpointEnabledAsync);
            ShowBreakpointPropertiesCommand = new RelayCommandAsync<BreakpointViewModel>(ShowBreakpointPropertiesAsync);
            viceBridge.ViceResponse += ViceBridge_ViceResponse;
            globals.PropertyChanged += Globals_PropertyChanged;
            executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
        }

        void Globals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Globals.Project):
                    _ = RemoveAllBreakpointsAsync();
                    break;
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
            var newBreakpoints = new List<BreakpointViewModel>(breakpoints.Length);
            foreach (var breakpoint in breakpoints)
            {
                bool isBreakpointReapplied = false;
                // first reapply breakpoints bound to a label
                if (breakpoint.Label is not null)
                {
                    var label = acmePdbManager.FindLabel(breakpoint.Label.Name);
                    if (label is not null)
                    {
                        var line = acmePdbManager.FindLineUsingAddress(breakpoint.Label.Address);
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
                    var match = FindMatchingLine(globals.Pdb, breakpoint.File, breakpoint.Line);
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
                            breakpoint.File.RelativePath, breakpoint.Line.LineNumber, breakpoint.Line.Text);
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
        internal static (AcmeFile File, AcmeLine Line)? FindMatchingLine(AcmePdb? pdb, AcmeFile originalFile, AcmeLine originalLine)
        {
            const int maxDelta = 5;
            const int minScoreToMatch = 90;
            if (pdb is null)
            {
                return null;
            }
            if (!pdb.Files.TryGetValue(originalFile.RelativePath, out var newFile))
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
        internal static AcmeLine? FuzzyFindLine(int maxDelta, int minScoreToMatch, AcmeLine originalLine, ImmutableArray<AcmeLine> lines)
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
        internal static ImmutableArray<AcmeLine> GetCandidatesForFuzzySearch(int lineIndex, int maxDelta, ImmutableArray<AcmeLine> lines)
        {
            int minBounds = Math.Max(0, lineIndex - maxDelta);
            int maxBounds = Math.Min(lines.Length - 1, lineIndex + maxDelta);
            int maxSteps = Math.Max(lineIndex - minBounds, maxBounds - lineIndex);
            var result = new List<AcmeLine>(maxSteps);
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
                    UpdateBreakpointDataFromVice(checkpointInfo);
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
        public async Task AddBreakpointForLabelAsync(AcmeLabel label, string? condition, CancellationToken ct = default)
        {
            // doesn't make sense that there is no line for given label's address
            var line = acmePdbManager.FindLineUsingAddress(label.Address)!;
            if (line.StartAddress is not null)
            {
                if (!breakpointsLinesMap.TryGetValue(line, out var breakpoint))
                {
                    var file = acmePdbManager.FindFileOfLine(line)!;
                    int lineNumber = file.Lines.IndexOf(line);
                    await AddBreakpointAsync(true, true, BreakpointMode.Exec, line, lineNumber, file, label, line.StartAddress.Value, line.EndAddress!.Value, null);
                }
                // in case breakpoint at that line already exists, just update it's Label property
                else
                {
                    breakpoint.Label = label;
                }
            }
        }
        public async Task AddBreakpointAsync(AcmeFile file, AcmeLine line, int lineNumber, AcmeLabel? label, string? condition, CancellationToken ct = default)
        {
            if (line.StartAddress is not null)
            {
                if (!breakpointsLinesMap.TryGetValue(line, out var breakpoint))
                {
                    await AddBreakpointAsync(true, true, BreakpointMode.Exec, line, lineNumber, file, label, line.StartAddress.Value, line.EndAddress!.Value, null);
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
        /// Wrapper around the other <see cref="AddBreakpointAsync(bool, bool, BreakpointMode, AcmeLine?, int?, AcmeFile?, AcmeLabel?, ushort, ushort, string?, CancellationToken)"/>
        /// </summary>
        /// <param name="breakpoint"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task<BreakpointViewModel?> AddBreakpointAsync(BreakpointViewModel breakpoint, CancellationToken ct = default)
        {
            return await AddBreakpointAsync(breakpoint.StopWhenHit, breakpoint.IsEnabled, breakpoint.Mode,
                        breakpoint.Line, breakpoint.LineNumber - 1, breakpoint.File, breakpoint.Label,
                        breakpoint.StartAddress, breakpoint.EndAddress, breakpoint.Condition, ct);
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
        internal async Task<BreakpointViewModel?> AddBreakpointAsync(bool stopWhenHit, bool isEnabled, BreakpointMode mode,
            AcmeLine? line, int? lineNumber, AcmeFile? file, AcmeLabel? label, ushort startAddress, ushort endAddress, string? condition, 
            CancellationToken ct = default)
        {
            var checkpointSetCommand = viceBridge.EnqueueCommand(
                       new CheckpointSetCommand(startAddress, endAddress, stopWhenHit, isEnabled, CpuOperation.Exec, false));
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
                        return default;
                    }
                }
                var breakpoint = new BreakpointViewModel(checkpointSetResponse.CheckpointNumber, checkpointSetResponse.StopWhenHit, checkpointSetResponse.Enabled, mode,
                            line, lineNumber+1, file, label,
                            checkpointSetResponse.StartAddress, checkpointSetResponse.EndAddress, condition);
                Breakpoints.Add(breakpoint);
                if (line is not null)
                {
                    breakpointsLinesMap[line] = breakpoint;
                }
                breakpointsMap.Add(breakpoint.CheckpointNumber, breakpoint);
                return breakpoint;
            }
            return default;
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
        public async Task<BreakpointViewModel> UpdateBreakpointAsync(BreakpointViewModel breakpoint, CancellationToken ct = default)
        {
            bool result = await RemoveBreakpointAsync(breakpoint, forceRemove: false, ct);
            if (!result)
            {
                throw new Exception("Failed to remove breakpoint from the list");
            }
            var newBreakpoint = await AddBreakpointAsync(breakpoint) ?? throw new Exception("Failed to set condition");
            return newBreakpoint;
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
}
