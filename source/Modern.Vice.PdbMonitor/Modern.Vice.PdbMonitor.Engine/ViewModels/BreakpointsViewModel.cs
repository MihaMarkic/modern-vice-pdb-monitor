using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Models;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
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
        public ObservableCollection<BreakpointViewModel> Breakpoints { get; }
        readonly Dictionary<AcmeLine, BreakpointViewModel> breakpointsLinesMap;
        readonly Dictionary<uint, BreakpointViewModel> breakpointsMap;
        public RelayCommandAsync<BreakpointViewModel> ToggleBreakpointEnabledCommand { get; }
        public bool IsWorking { get; private set; }
        public BreakpointsViewModel(ILogger<RegistersViewModel> logger, IViceBridge viceBridge, IDispatcher dispatcher, ExecutionStatusViewModel executionStatusViewModel)
        {
            this.logger = logger;
            this.viceBridge = viceBridge;
            this.dispatcher = dispatcher;
            this.executionStatusViewModel = executionStatusViewModel;
            Breakpoints = new ObservableCollection<BreakpointViewModel>();
            breakpointsLinesMap = new Dictionary<AcmeLine, BreakpointViewModel>();
            breakpointsMap = new Dictionary<uint, BreakpointViewModel>();
            ToggleBreakpointEnabledCommand = new RelayCommandAsync<BreakpointViewModel>(ToggleBreakpointEnabledAsync);
            viceBridge.ViceResponse += ViceBridge_ViceResponse;
            executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
        }

        void ExecutionStatusViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ExecutionStatusViewModel.IsDebugging) when !executionStatusViewModel.IsDebugging:
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
        public async Task ReapplyBreakpoints(CancellationToken ct)
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
            // reapply breakpoints
            foreach (var breakpoint in breakpoints)
            {
                // TODO handle PDB changes
                await AddBreakpointAsync(breakpoint.StopWhenHit, breakpoint.IsEnabled, breakpoint.Line, breakpoint.LineNumber - 1, breakpoint.File,
                    breakpoint.StartAddress, breakpoint.EndAddress, breakpoint.Condition, ct);
            }
            logger.LogDebug("Checkpoints reapplied"); 
        }
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
        public async Task RemoveBreakpointAsync(BreakpointViewModel breakpoint, CancellationToken ct = default)
        {
            bool success = await DeleteCheckpointAsync(breakpoint.CheckpointNumber, ct);
            if (success)
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
        public AcmeLine? Line { get; }
        public AcmeFile? File { get; }
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
