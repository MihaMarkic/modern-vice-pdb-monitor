using System;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class BreakpointDetailViewModel: NotifiableObject, IDialogViewModel<SimpleDialogResult>
    {
        readonly ILogger<BreakpointDetailViewModel> logger;
        readonly BreakpointsViewModel breakpoints;
        public BreakpointViewModel Breakpoint { get; }
        public Action<SimpleDialogResult>? Close { get; set; }
        public RelayCommand CancelCommand { get; }

        public BreakpointDetailViewModel(ILogger<BreakpointDetailViewModel> logger, BreakpointsViewModel breakpoints, BreakpointViewModel breakpoint)
        {
            this.logger = logger;
            this.breakpoints = breakpoints;
            Breakpoint = breakpoint;
            CancelCommand = new RelayCommand(Cancel);
        }

        void Cancel()
        {
            Close?.Invoke(new SimpleDialogResult(DialogResultCode.Cancel));
        }
    }
}
