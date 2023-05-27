using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.BindingValidators;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using PropertyChanged;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class BreakpointDetailViewModel: NotifiableObject, IDialogViewModel<SimpleDialogResult>, INotifyDataErrorInfo
{
    readonly ILogger<BreakpointDetailViewModel> logger;
    readonly BreakpointsViewModel breakpoints;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    readonly BreakpointViewModel sourceBreakpoint;
    public string? SaveError { get; private set; }
    public BreakpointViewModel Breakpoint { get; }
    public Action<SimpleDialogResult>? Close { get; set; }
    public RelayCommandAsync SaveCommand { get; }
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
    public bool HasApplyButton => false; // Mode == BreakpointDetailDialogMode.Update; // disabled for now, perhaps enabled in the future
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
        SaveCommand = new RelayCommandAsync(SaveAsync, CanSave);
        CreateCommand = new RelayCommandAsync(CreateAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel);
        ApplyCommand = new RelayCommand(Apply, CanSave);
        Breakpoint.PropertyChanged += Breakpoint_PropertyChanged;
        UnbindToFileCommand = new RelayCommand(UnbindToFile, () => Breakpoint.Label is not null);
        FullUnbindCommand = new RelayCommand(FullUnbind, () => IsBreakpointBound);

        startAddressValidator = new HexValidator(nameof(StartAddress), Breakpoint.StartAddress, 4, a => Breakpoint.StartAddress = a ?? 0);
        endAddressValidator = new HexValidator(nameof(EndAddress), Breakpoint.EndAddress, 4, a => Breakpoint.EndAddress = a ?? 0);
        validators = ImmutableArray<IBindingValidator>.Empty.Add(startAddressValidator).Add(endAddressValidator);
        foreach (var validator in validators)
        {
            validator.HasErrorsChanged += ValidatorHasErrorsChanged;
        }
    }
    internal bool CanSave() => !HasErrors && Breakpoint.IsChangedFrom(sourceBreakpoint);
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
    [SuppressPropertyChangedWarnings]
    void OnErrorsChanged(DataErrorsChangedEventArgs e) => ErrorsChanged?.Invoke(this, e);
    void ValidatorHasErrorsChanged(object? sender, EventArgs e)
    {
        var validator = (IBindingValidator)sender!;
        OnErrorsChanged(new DataErrorsChangedEventArgs(validator.SourcePropertyName));
    }
    void Breakpoint_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        HasChanges = true;
        SaveError = null;
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
            case nameof(StartAddress):
            case nameof(EndAddress):
                // validates start and end address, not very efficient since it does it on every change
                // TODO improve
                OnErrorsChanged(new DataErrorsChangedEventArgs(nameof(EndAddress)));
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
        if (!startAddressValidator.HasErrors && !endAddressValidator.HasErrors)
        {
            if (Breakpoint.EndAddress <= Breakpoint.StartAddress)
            {
                errors.Add("End Address should be higher than Start Address");
            }
        }
        HasErrors = errors.Count > 0;
        return errors.ToImmutableArray();
    }
    async Task SaveAsync()
    {
        try
        {
            await breakpoints.UpdateBreakpointAsync(Breakpoint);
            Close?.Invoke(new SimpleDialogResult(DialogResultCode.OK));
        }
        catch (Exception ex)
        {
            SaveError = $"Failed saving breakpoint: {ex.Message}";
        }
    }
    async Task CreateAsync()
    {
        try
        {
            await breakpoints.AddBreakpointAsync(Breakpoint, CancellationToken.None);
            Close?.Invoke(new SimpleDialogResult(DialogResultCode.OK));
        }
        catch (Exception ex)
        {
            SaveError = $"Failed creating breakpoint: {ex.Message}";
        }
    }
    void Cancel()
    {
        Close?.Invoke(new SimpleDialogResult(DialogResultCode.Cancel));
    }

    void Apply()
    {
        // not implemented at this time
    }

    protected override void OnPropertyChanged(string name = null!)
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
