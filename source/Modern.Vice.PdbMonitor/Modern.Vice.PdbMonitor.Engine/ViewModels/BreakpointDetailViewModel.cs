using System.Collections;
using System.ComponentModel;
using System.Net;
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
    readonly Globals globals;
    readonly BreakpointsViewModel breakpoints;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    readonly BreakpointViewModel sourceBreakpoint;
    public string? SaveError { get; private set; }
    public BreakpointViewModel Breakpoint { get; }
    public Action<SimpleDialogResult>? Close { get; set; }
    public RelayCommandAsync SaveCommand { get; }
    public RelayCommandAsync CreateCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ClearBindingCommand { get; }
    public RelayCommand ConvertBindingToGlobalVariableCommand { get; }
    //public RelayCommand UnbindToFileCommand { get; }
    //public RelayCommand FullUnbindCommand { get; }
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
    public string? EndAddress
    {
        get => endAddressValidator.TextValue;
        set => endAddressValidator.UpdateText(value);
    }
    readonly HexValidator endAddressValidator;
    readonly HigherThanBindingValidator endAddressHigherThanStartValidator;
    /// <summary>
    /// Used for binding to global variable
    /// </summary>
    public PdbVariable? GlobalVariable { get; set; }
    readonly PdbVariableBindValidator globalVariableBindValidator;
    public bool IsAddressRangeReadOnly => IsBreakpointBound;
    public bool IsBreakpointBound => Breakpoint.Bind is not BreakpointNoBind;
    public bool IsModeEnabled => Breakpoint.Bind is not BreakpointLineBind;
    public bool IsExecModeEnabled => Breakpoint.Bind is BreakpointLineBind || Breakpoint.Bind is BreakpointNoBind;
    public bool IsLoadStoreModeEnabled => Breakpoint.Bind is not BreakpointLineBind;
    public ImmutableArray<PdbVariable> GlobalVariables { get; }
    public ImmutableDictionary<string, PdbVariable> GlobalVariablesMap { get; }
    readonly ImmutableDictionary<string, ImmutableArray<IBindingValidator>> validators;
    public BreakpointDetailViewModel(ILogger<BreakpointDetailViewModel> logger, Globals globals,
        BreakpointsViewModel breakpoints, BreakpointViewModel breakpoint, BreakpointDetailDialogMode mode)
    {
        this.logger = logger;
        this.globals = globals;
        this.breakpoints = breakpoints;
        sourceBreakpoint = breakpoint;
        Breakpoint = breakpoint.Clone();
        Mode = mode;
        SaveCommand = new RelayCommandAsync(SaveAsync, CanSave);
        CreateCommand = new RelayCommandAsync(CreateAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel);
        ClearBindingCommand = new RelayCommand(ClearBinding, () => Breakpoint.Bind is not BreakpointNoBind);
        ConvertBindingToGlobalVariableCommand = new RelayCommand(ConvertBindingToGlobalVariable);
        Breakpoint.PropertyChanged += Breakpoint_PropertyChanged;

        GlobalVariables = globals.Project?.DebugSymbols?.GlobalVariables.Values.ToImmutableArray() 
            ?? ImmutableArray<PdbVariable>.Empty;
        GlobalVariablesMap = globals.Project?.DebugSymbols?.GlobalVariables
            ?? ImmutableDictionary<string, PdbVariable>.Empty;

        startAddressValidator = new HexValidator(nameof(StartAddress), 0, 4, a => UpdateStartAddress(a ?? 0));
        endAddressValidator = new HexValidator(nameof(EndAddress), 0, 4, a => UpdateEndAddress(a ?? 0));
        endAddressHigherThanStartValidator = new HigherThanBindingValidator(nameof(EndAddress));
        globalVariableBindValidator = new PdbVariableBindValidator(nameof(GlobalVariable), UpdateGlobalVariable);
        validators = ImmutableDictionary<string, ImmutableArray<IBindingValidator>>
            .Empty
            .Add(nameof(StartAddress), ImmutableArray<IBindingValidator>.Empty.Add(startAddressValidator))
            .Add(nameof(EndAddress), ImmutableArray<IBindingValidator>.Empty
                .Add(endAddressValidator).Add(endAddressHigherThanStartValidator))
            .Add(nameof(GlobalVariable), ImmutableArray<IBindingValidator>.Empty.Add(globalVariableBindValidator));
        // bind all validators
        foreach (var validator in validators.Values.SelectMany(a => a))
        {
            validator.HasErrorsChanged += ValidatorHasErrorsChanged;
        }

        switch (Breakpoint.Bind)
        {
            case BreakpointNoBind noBind:
                UpdateNoBindAddressesFromViewModel(noBind);
                startAddressValidator.UpdateText(StartAddress);
                endAddressValidator.UpdateText(EndAddress);
                endAddressHigherThanStartValidator.Update(noBind);
                break;
            case BreakpointGlobalVariableBind globalVariableBind:
                GlobalVariable = GlobalVariablesMap.TryGetValue(globalVariableBind.VariableName, out var tempGlobalVariable)
                    ? tempGlobalVariable : null;
                globalVariableBindValidator.Update(GlobalVariable);
                break;
        }
    }
    void UpdateNoBindAddressesFromViewModel(BreakpointNoBind noBind)
    {
        StartAddress = startAddressValidator.ConvertTo(noBind.StartAddress);
        EndAddress = endAddressValidator.ConvertTo(noBind.EndAddress);
    }
    void UpdateStartAddress(ushort address)
    {
        if (Breakpoint.Bind is BreakpointNoBind noBind)
        {
            Breakpoint.Bind = noBind with
            {
                StartAddress = address,
            };
        }
    }
    void UpdateEndAddress(ushort address)
    {
        if (Breakpoint.Bind is BreakpointNoBind noBind)
        {
            Breakpoint.Bind = noBind with
            {
                EndAddress = address,
            };
        }
    }
    void UpdateGlobalVariable(PdbVariable variable)
    {
        if (Breakpoint.Bind is BreakpointGlobalVariableBind globalVariableBind)
        {
            Breakpoint.Bind = globalVariableBind with
            {
                VariableName = variable.Name,
            };
            Breakpoint.AddressRanges = ImmutableHashSet<BreakpointAddressRange>.Empty
                .Add(new BreakpointAddressRange((ushort)variable.Start, (ushort)variable.End));
        }
    }
    internal void ClearBinding()
    {
        Breakpoint.Bind = BreakpointNoBind.Empty;
        UpdateNoBindAddressesFromViewModel(BreakpointNoBind.Empty);
    }
    internal void ConvertBindingToGlobalVariable()
    {
        Breakpoint.Bind = new BreakpointGlobalVariableBind("");
        if (Breakpoint.Mode == BreakpointMode.Exec)
        {
            Breakpoint.Mode = BreakpointMode.Load;
        }
    }
    internal bool CanSave() => !HasErrors && Breakpoint.IsChangedFrom(sourceBreakpoint);
    [SuppressPropertyChangedWarnings]
    void OnErrorsChanged(DataErrorsChangedEventArgs e) => ErrorsChanged?.Invoke(this, e);
    void ValidatorHasErrorsChanged(object? sender, EventArgs e)
    {
        var validator = (IBindingValidator)sender!;
        HasErrors = validators.Values
            .SelectMany(a => a)
            .Where(v => v.HasErrors)
            .Any();
        OnErrorsChanged(new DataErrorsChangedEventArgs(validator.SourcePropertyName));
    }
    void Breakpoint_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        HasChanges = true;
        SaveError = null;
        SaveCommand.RaiseCanExecuteChanged();
        CreateCommand.RaiseCanExecuteChanged();
        switch (e.PropertyName)
        {
            case nameof(BreakpointViewModel.Bind):
                ClearBindingCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(IsBreakpointBound));
                OnPropertyChanged(nameof(IsModeEnabled));
                OnPropertyChanged(nameof(IsAddressRangeReadOnly));
                OnPropertyChanged(nameof(IsExecModeEnabled));
                OnPropertyChanged(nameof(IsLoadStoreModeEnabled));
                endAddressHigherThanStartValidator.Update(Breakpoint.Bind as BreakpointNoBind);
                switch (Breakpoint.Bind)
                {
                    case BreakpointNoBind noBind:
                        Breakpoint.AddressRanges = ImmutableHashSet<BreakpointAddressRange>
                            .Empty.Add(new(noBind.StartAddress, noBind.EndAddress));
                        break;
                    case BreakpointGlobalVariableBind variableBind:
                        Breakpoint.AddressRanges = ImmutableHashSet<BreakpointAddressRange>.Empty;
                        globalVariableBindValidator.Update(null);
                        break;
                };
                // clear no bind (address) validators when type is changed
                if (Breakpoint.Bind is not BreakpointNoBind)
                {
                    startAddressValidator.Clear();
                    endAddressValidator.Clear();
                    endAddressHigherThanStartValidator.Clear();
                }
                if (Breakpoint.Bind is not BreakpointGlobalVariableBind)
                {
                    GlobalVariable = null;
                }
                break;
        }
    }
    public IEnumerable GetErrors(string? propertyName)
    {
        if (!string.IsNullOrEmpty(propertyName) && validators.TryGetValue(propertyName, out var propertyValidators))
        {
            var errors = new List<string>();
            foreach (var pv in propertyValidators)
            {
                errors.AddRange(pv.Errors);
            }
            HasErrors = errors.Count > 0;
            return errors.ToImmutableArray();
        }
        else
        {
            return Enumerable.Empty<string>();
        }
    }
    async Task SaveAsync()
    {
        try
        {
            await breakpoints.UpdateBreakpointAsync(Breakpoint, sourceBreakpoint);
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

    protected override void OnPropertyChanged(string name = null!)
    {
        base.OnPropertyChanged(name);
        switch (name)
        {
            case nameof(HasErrors):
                SaveCommand.RaiseCanExecuteChanged();
                CreateCommand.RaiseCanExecuteChanged();
                break;
            case nameof(GlobalVariable):
                globalVariableBindValidator.Update(GlobalVariable);
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
