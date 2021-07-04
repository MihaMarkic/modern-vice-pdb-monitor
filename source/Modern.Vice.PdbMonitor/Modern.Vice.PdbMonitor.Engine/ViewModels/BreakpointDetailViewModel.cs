using System;
using System.Threading;
using System.Threading.Tasks;
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
        public RelayCommand SaveCommand { get; }
        public RelayCommandAsync CreateCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand ApplyCommand { get; }
        public BreakpointDetailDialogMode Mode { get; }
        public bool HasCreateButton => Mode == BreakpointDetailDialogMode.Create;
        public bool HasApplyButton => Mode == BreakpointDetailDialogMode.Update;
        public bool HasSaveButton => Mode == BreakpointDetailDialogMode.Update;
        public BreakpointDetailViewModel(ILogger<BreakpointDetailViewModel> logger, BreakpointsViewModel breakpoints, BreakpointViewModel breakpoint,
            BreakpointDetailDialogMode mode)
        {
            this.logger = logger;
            this.breakpoints = breakpoints;
            Breakpoint = breakpoint;
            Mode = mode;
            SaveCommand = new RelayCommand(Save);
            CreateCommand = new RelayCommandAsync(CreateAsync);
            CancelCommand = new RelayCommand(Cancel);
            ApplyCommand = new RelayCommand(Apply);
        }

        void Save()
        {
            Close?.Invoke(new SimpleDialogResult(DialogResultCode.OK));
        }
        async Task CreateAsync()
        {
            await breakpoints.DeleteCheckpointAsync(Breakpoint.CheckpointNumber, CancellationToken.None);
            if (Breakpoint.File is not null && Breakpoint.Line is not null)
            {
                await breakpoints.AddBreakpointAsync(Breakpoint.File, Breakpoint.Line, Breakpoint.LineNumber!.Value, Breakpoint.Label, Breakpoint.Condition);
            }
            Close?.Invoke(new SimpleDialogResult(DialogResultCode.OK));
        }
        void Cancel()
        {
            Close?.Invoke(new SimpleDialogResult(DialogResultCode.Cancel));
        }
        void Apply()
        {

        }
    }

    public enum BreakpointDetailDialogMode
    {
        Create,
        Update
    }
}
