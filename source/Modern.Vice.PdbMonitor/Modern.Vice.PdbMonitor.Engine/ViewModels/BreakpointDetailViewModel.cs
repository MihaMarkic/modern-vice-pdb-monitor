using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.BindingValidators;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class BreakpointDetailViewModel: NotifiableObject, IDialogViewModel<SimpleDialogResult>, INotifyDataErrorInfo
    {
        readonly ILogger<BreakpointDetailViewModel> logger;
        readonly BreakpointsViewModel breakpoints;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        readonly BreakpointViewModel sourceBreakpoint;
        public BreakpointViewModel Breakpoint { get; }
        public Action<SimpleDialogResult>? Close { get; set; }
        public RelayCommand SaveCommand { get; }
        public RelayCommandAsync CreateCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand ApplyCommand { get; }
        public RelayCommand UnbindToFileCommand { get; }
        public RelayCommand FullUnbindCommand { get; }
        public BreakpointDetailDialogMode Mode { get; }
        public bool HasChanges { get; private set; }
        public bool HasErrors { get; private set; }
        public bool CanUpdate => HasChanges && !HasErrors;
        public bool HasCreateButton => Mode == BreakpointDetailDialogMode.Create;
        public bool HasApplyButton => Mode == BreakpointDetailDialogMode.Update;
        public bool HasSaveButton => Mode == BreakpointDetailDialogMode.Update;
        /// <summary>
        /// Proxy to <see cref="BreakpointViewModel.StartAddress"/> through <see cref="startAddressValidator"/>.
        /// </summary>
        public string? StartAddress
        {
            get => startAddressValidator.TextValue;
            set => startAddressValidator.UpdateText(value);
        }
        readonly HexValidator startAddressValidator;
        public bool IsStartAddressReadOnly => IsBreakpointBound;
        public string? EndAddress
        {
            get => endAddressValidator.TextValue;
            set => endAddressValidator.UpdateText(value);
        }
        readonly HexValidator endAddressValidator;
        public bool IsEndAddressReadOnly => IsBreakpointBound;
        public bool IsBreakpointBound => Breakpoint.FileName is not null;
        readonly ImmutableArray<IBindingValidator> validators;
        public BreakpointDetailViewModel(ILogger<BreakpointDetailViewModel> logger, BreakpointsViewModel breakpoints, BreakpointViewModel breakpoint,
            BreakpointDetailDialogMode mode)
        {
            this.logger = logger;
            this.breakpoints = breakpoints;
            sourceBreakpoint = breakpoint;
            Breakpoint = breakpoint.Clone();
            Mode = mode;
            SaveCommand = new RelayCommand(Save, () => !HasErrors && breakpoint.IsChangedFrom(sourceBreakpoint));
            CreateCommand = new RelayCommandAsync(CreateAsync, () => !HasErrors && breakpoint.IsChangedFrom(sourceBreakpoint));
            CancelCommand = new RelayCommand(Cancel);
            ApplyCommand = new RelayCommand(Apply, () => !HasErrors && breakpoint.IsChangedFrom(sourceBreakpoint));
            breakpoint.PropertyChanged += Breakpoint_PropertyChanged;
            UnbindToFileCommand = new RelayCommand(UnbindToFile, () => Breakpoint.Label is not null);
            FullUnbindCommand = new RelayCommand(FullUnbind, () => IsBreakpointBound);

            startAddressValidator = new HexValidator(nameof(StartAddress), Breakpoint.StartAddress, 4, a => breakpoint.StartAddress = a);
            endAddressValidator = new HexValidator(nameof(EndAddress), Breakpoint.EndAddress, 4, a => breakpoint.EndAddress = a);
            validators = ImmutableArray<IBindingValidator>.Empty.Add(startAddressValidator).Add(endAddressValidator);
            foreach (var validator in validators)
            {
                validator.HasErrorsChanged += ValidatorHasErrorsChanged;
            }
        }
        void UnbindToFile()
        {
            Breakpoint.Label = null;
        }
        void FullUnbind()
        {
            UnbindToFile();
            Breakpoint.File = null;
            Breakpoint.Line = null;
            Breakpoint.LineNumber = null;
        }
        void OnErrorsChanged(DataErrorsChangedEventArgs e) => ErrorsChanged?.Invoke(this, e);
        void ValidatorHasErrorsChanged(object? sender, EventArgs e)
        {
            var validator = (IBindingValidator)sender!;
            OnErrorsChanged(new DataErrorsChangedEventArgs(validator.SourcePropertyName));
        }
        void Breakpoint_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            HasChanges = true;
            SaveCommand.RaiseCanExecuteChanged();
            CreateCommand.RaiseCanExecuteChanged();
            ApplyCommand.RaiseCanExecuteChanged();
            switch (e.PropertyName)
            {
                case nameof(BreakpointViewModel.File):
                case nameof(BreakpointViewModel.Label):
                    UnbindToFileCommand.RaiseCanExecuteChanged();
                    FullUnbindCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(IsBreakpointBound));
                    OnPropertyChanged(nameof(IsStartAddressReadOnly));
                    OnPropertyChanged(nameof(IsEndAddressReadOnly));
                    break;
            }
        }
        public IEnumerable GetErrors(string? propertyName)
        {
            var errors = new List<string>();
            foreach (var validator in validators)
            {
                errors.AddRange(validator.Errors);
            }
            return errors.ToImmutableArray();
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

        protected override void OnPropertyChanged([CallerMemberName] string name = null)
        {
            base.OnPropertyChanged(name);
            switch (name)
            {
                case nameof(HasErrors):
                    SaveCommand.RaiseCanExecuteChanged();
                    ApplyCommand.RaiseCanExecuteChanged();
                    CreateCommand.RaiseCanExecuteChanged();
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Breakpoint.PropertyChanged -= Breakpoint_PropertyChanged;
            }
            base.Dispose(disposing);
        }
    }

    public enum BreakpointDetailDialogMode
    {
        Create,
        Update
    }
}
