using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Models;
using Righthand.ViceMonitor.Bridge.Services.Abstract;
using System;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using System.Threading;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class BreakpointsViewModel: NotifiableObject
    {
        readonly ILogger<RegistersViewModel> logger;
        readonly IViceBridge viceBridge;
        public ObservableCollection<BreakpointViewModel> Breakpoints { get; }
        ImmutableDictionary<ushort, BreakpointViewModel> map;
        public RelayCommandAsync<BreakpointViewModel> ToggleBreakpointEnabledCommand { get; }
        public BreakpointsViewModel(ILogger<RegistersViewModel> logger, IViceBridge viceBridge)
        {
            this.logger = logger;
            this.viceBridge = viceBridge;
            Breakpoints = new ObservableCollection<BreakpointViewModel>();
            map = ImmutableDictionary<ushort, BreakpointViewModel>.Empty;
            ToggleBreakpointEnabledCommand = new RelayCommandAsync<BreakpointViewModel>(ToggleBreakpointEnabledAsync);
            //Breakpoints.Add(new BreakpointViewModel(true, default!, 0x1300));
            //Breakpoints.Add(new BreakpointViewModel(false, default!, 0xff00));
        }
        public async Task AddBreakpointAsync(AcmeFile file, AcmeLine line, string? condition, CancellationToken ct = default)
        {
            if (line.StartAddress is not null)
            {
                ushort address = line.StartAddress.Value;
                if (!map.TryGetValue(address, out var breakpoint))
{
                    breakpoint = new BreakpointViewModel(isEnabled: true, file, line, address);
                    await AddBreakpointAsync(breakpoint, ct);
                }
                breakpoint.Condition = condition;
            }
        }
        internal async Task ToggleBreakpointEnabledAsync(BreakpointViewModel? breakpoint)
        {
            if (breakpoint is not null)
            {
                breakpoint.IsEnabled = !breakpoint.IsEnabled;
            }
        }
        internal async Task AddBreakpointAsync(BreakpointViewModel breakpoint, CancellationToken ct = default)
        {
            Breakpoints.Add(breakpoint);
            map.Add(breakpoint.Address, breakpoint);
        }
        public async Task RemoveBreakpointAsync(BreakpointViewModel breakpoint, CancellationToken ct = default)
        {
            Breakpoints.Remove(breakpoint);
            map.Remove(breakpoint.Address);
        }
    }

    public class BreakpointViewModel: NotifiableObject
    {
        public bool IsEnabled { get; set; }
        public AcmeLine Line { get; }
        public AcmeFile File { get; }
        public ushort Address { get; set; }
        public string? Condition { get; set; }
        public BreakpointViewModel(bool isEnabled, AcmeFile file, AcmeLine line, ushort address)
        {
            IsEnabled = isEnabled;
            File = file;
            Line = line;
            Address = address;
        }
    }
}
