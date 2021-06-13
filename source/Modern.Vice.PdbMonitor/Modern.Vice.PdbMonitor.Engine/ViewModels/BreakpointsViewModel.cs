using FuzzySharp;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Models;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class BreakpointsViewModel: NotifiableObject
    {
        readonly ILogger<RegistersViewModel> logger;
        readonly IDispatcher dispatcher;
        readonly IViceBridge viceBridge;
        readonly ExecutionStatusViewModel executionStatusViewModel;
        readonly Globals globals;
        public ObservableCollection<BreakpointViewModel> Breakpoints { get; }
        readonly Dictionary<AcmeLine, BreakpointViewModel> breakpointsLinesMap;
        readonly Dictionary<uint, BreakpointViewModel> breakpointsMap;
        public RelayCommandAsync<BreakpointViewModel> ToggleBreakpointEnabledCommand { get; }
        public bool IsWorking { get; private set; }
        public BreakpointsViewModel(ILogger<RegistersViewModel> logger, IViceBridge viceBridge, IDispatcher dispatcher, Globals globals,
            ExecutionStatusViewModel executionStatusViewModel)
        {
            this.logger = logger;
            this.viceBridge = viceBridge;
            this.dispatcher = dispatcher;
            this.globals = globals;
            this.executionStatusViewModel = executionStatusViewModel;
            Breakpoints = new ObservableCollection<BreakpointViewModel>();
            breakpointsLinesMap = new Dictionary<AcmeLine, BreakpointViewModel>();
            breakpointsMap = new Dictionary<uint, BreakpointViewModel>();
            ToggleBreakpointEnabledCommand = new RelayCommandAsync<BreakpointViewModel>(ToggleBreakpointEnabledAsync);
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
                    await AddBreakpointAsync(breakpoint.StopWhenHit, breakpoint.IsEnabled, breakpoint.Line, breakpoint.LineNumber - 1, breakpoint.File,
                        breakpoint.StartAddress, breakpoint.EndAddress, breakpoint.Condition, ct);
                }
            }
        }
        internal async Task ApplyOriginalBreakpointsOnNewPdbAsync(ImmutableArray<BreakpointViewModel> breakpoints, CancellationToken ct)
        {
            var newBreakpoints = new List<BreakpointViewModel>(breakpoints.Length);
            foreach (var breakpoint in breakpoints)
            {
                if (breakpoint.File is not null && breakpoint.Line is not null)
                {
                    var match = FindMatchingLine(globals.Pdb, breakpoint.File, breakpoint.Line);
                    if (match is not null)
                    {
                        breakpoint.Line = match.Value.Line;
                        breakpoint.File = match.Value.File;
                        newBreakpoints.Add(breakpoint);
                    }
                }
                // for now set all non-line breakpoints as well
                else
                {
                    newBreakpoints.Add(breakpoint);
                }
                await AddBreakpointAsync(breakpoint.StopWhenHit, breakpoint.IsEnabled, breakpoint.Line, breakpoint.LineNumber - 1, breakpoint.File,
                    breakpoint.StartAddress, breakpoint.EndAddress, breakpoint.Condition, ct);
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
                    UpdateBreakpoint(checkpointInfo);
                    break;
            }
        }

        void UpdateBreakpoint(CheckpointInfoResponse checkpointInfo)
        {
            if (breakpointsMap.TryGetValue(checkpointInfo.CheckpointNumber, out var breakpoint))
            {
                breakpoint.IsCurrentlyHit = checkpointInfo.CurrentlyHit;
                breakpoint.IsEnabled = checkpointInfo.Enabled;
                breakpoint.HitCount = checkpointInfo.HitCount;
                breakpoint.IgnoreCount = checkpointInfo.IgnoreCount;
            }
        }

        public async Task AddBreakpointAsync(AcmeFile file, AcmeLine line, int lineNumber, string? condition, CancellationToken ct = default)
        {
            if (line.StartAddress is not null)
            {
                if (!breakpointsLinesMap.TryGetValue(line, out var breakpoint))
                {
                    await AddBreakpointAsync(true, true, line, lineNumber, file, line.StartAddress.Value, line.EndAddress!.Value, null);
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
        internal async Task<BreakpointViewModel?> AddBreakpointAsync(bool stopWhenHit, bool isEnabled,
            AcmeLine? line, int? lineNumber, AcmeFile? file, ushort startAddress, ushort endAddress, string? condition, 
            CancellationToken ct = default)
        {
            var checkpointSetCommand = viceBridge.EnqueueCommand(
                       new CheckpointSetCommand(startAddress, endAddress, stopWhenHit, isEnabled, CpuOperation.Exec, false));
            var checkpointSetResponse = await checkpointSetCommand.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, checkpointSetCommand, ct: ct);
            if (checkpointSetResponse is not null)
            {
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
                var breakpoint = new BreakpointViewModel(checkpointSetResponse.CheckpointNumber, checkpointSetResponse.StopWhenHit, checkpointSetResponse.Enabled, 
                            line, lineNumber+1, file,
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
        public async Task RemoveBreakpointAsync(BreakpointViewModel breakpoint, bool forceRemove, CancellationToken ct = default)
        {
            bool success = await DeleteCheckpointAsync(breakpoint.CheckpointNumber, ct);
            if (success || forceRemove)
            {
                Breakpoints.Remove(breakpoint);
                if (breakpoint.Line is not null)
                {
                    breakpointsLinesMap.Remove(breakpoint.Line);
                }
                breakpointsMap.Remove(breakpoint.CheckpointNumber);
            }
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

    public class BreakpointViewModel: NotifiableObject
    {
        public uint CheckpointNumber { get; }
        public bool IsCurrentlyHit { get; set; }
        public bool StopWhenHit { get; set; }
        public bool IsEnabled { get; set; }
        public uint HitCount { get; set; }
        public uint IgnoreCount { get; set; }
        public AcmeLine? Line { get; set; }
        public AcmeFile? File { get; set; }
        public ushort StartAddress { get; }
        public ushort EndAddress { get; }
        public string? Condition { get; set; }
        public string? FileName => File?.RelativePath;
        public int? LineNumber { get; }
        public BreakpointViewModel(uint checkpointNumber, bool stopWhenHit, bool isEnabled, 
            AcmeLine? line, int? lineNumber, AcmeFile? file, ushort startAddress, ushort endAddress, string? condition)
        {
            CheckpointNumber = checkpointNumber;
            StopWhenHit = stopWhenHit;
            IsEnabled = isEnabled;
            Line = line;
            LineNumber = lineNumber;
            File = file;
            StartAddress = startAddress;
            EndAddress = endAddress;
            Condition = condition;
        }
    }
}
