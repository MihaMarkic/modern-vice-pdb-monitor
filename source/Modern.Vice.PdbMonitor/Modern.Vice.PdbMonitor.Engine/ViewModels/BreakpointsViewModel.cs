using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Models;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Services.Abstract;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class BreakpointsViewModel: NotifiableObject
    {
        readonly ILogger<RegistersViewModel> logger;
        readonly IDispatcher dispatcher;
        readonly IViceBridge viceBridge;
        public ObservableCollection<BreakpointViewModel> Breakpoints { get; }
        readonly Dictionary<AcmeLine, BreakpointViewModel> map;
        public RelayCommandAsync<BreakpointViewModel> ToggleBreakpointEnabledCommand { get; }
        public bool IsWorking { get; private set; }
        public BreakpointsViewModel(ILogger<RegistersViewModel> logger, IViceBridge viceBridge, IDispatcher dispatcher)
        {
            this.logger = logger;
            this.viceBridge = viceBridge;
            this.dispatcher = dispatcher;
            Breakpoints = new ObservableCollection<BreakpointViewModel>();
            map = new Dictionary<AcmeLine, BreakpointViewModel>();
            ToggleBreakpointEnabledCommand = new RelayCommandAsync<BreakpointViewModel>(ToggleBreakpointEnabledAsync);
            //Breakpoints.Add(new BreakpointViewModel(true, default!, 0x1300));
            //Breakpoints.Add(new BreakpointViewModel(false, default!, 0xff00));
        }
        public async Task AddBreakpointAsync(AcmeFile file, AcmeLine line, int lineNumber, string? condition, CancellationToken ct = default)
        {
            if (line.StartAddress is not null)
            {
                if (!map.TryGetValue(line, out var breakpoint))
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
                            line, lineNumber, file,
                            checkpointSetResponse.StartAddress, checkpointSetResponse.EndAddress, condition);
                Breakpoints.Add(breakpoint);
                if (breakpoint is not null && line is not null)
                {
                    map[line] = breakpoint;
                }
                return breakpoint;
            }
            return default;
        }
        public async Task RemoveBreakpointAsync(BreakpointViewModel breakpoint, CancellationToken ct = default)
        {
            var command = viceBridge.EnqueueCommand(new CheckpointDeleteCommand(breakpoint.CheckpointNumber));
            var result = await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command, ct: ct);
            if (result is not null)
            {
                Breakpoints.Remove(breakpoint);
                if (breakpoint.Line is not null)
                {
                    map.Remove(breakpoint.Line);
                }
            }
        }
    }

    public class BreakpointViewModel: NotifiableObject
    {
        public uint CheckpointNumber { get; }
        public bool IsCurrentlyHit { get; set; }
        public bool StopWhenHit { get; set; }
        public bool IsEnabled { get; set; }
        public ushort HitCount { get; set; }
        public ushort IgnoreCount { get; set; }
        public AcmeLine? Line { get; }
        public AcmeFile? File { get; }
        public ushort StartAddress { get; set; }
        public ushort EndAddress { get; set; }
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
