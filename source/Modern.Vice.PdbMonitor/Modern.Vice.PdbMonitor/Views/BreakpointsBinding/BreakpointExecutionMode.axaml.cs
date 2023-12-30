using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Models;

namespace Modern.Vice.PdbMonitor.Views.BreakpointsBinding;

public partial class BreakpointExecutionMode : UserControl
{
    public static readonly DirectProperty<BreakpointExecutionMode, BreakpointMode> ModeProperty = 
        AvaloniaProperty.RegisterDirect<BreakpointExecutionMode, BreakpointMode>(nameof(Mode),
        o => o.Mode, 
        (o, v) => 
        {
            o.Mode = v;
            o.UpdateClasses();
        }, 
        defaultBindingMode: BindingMode.TwoWay);
    public static readonly DirectProperty<BreakpointExecutionMode, bool> IsExecEnabledProperty =
        AvaloniaProperty.RegisterDirect<BreakpointExecutionMode, bool>(nameof(IsExecEnabled),
        o => o.IsExecEnabled,
        (o, v) => o.IsExecEnabled = v,
        defaultBindingMode: BindingMode.OneWay);
    public static readonly DirectProperty<BreakpointExecutionMode, bool> IsLoadEnabledProperty =
        AvaloniaProperty.RegisterDirect<BreakpointExecutionMode, bool>(nameof(IsLoadEnabled),
        o => o.IsLoadEnabled,
        (o, v) => o.IsLoadEnabled = v,
        defaultBindingMode: BindingMode.OneWay);
    public static readonly DirectProperty<BreakpointExecutionMode, bool> IsStoreEnabledProperty =
        AvaloniaProperty.RegisterDirect<BreakpointExecutionMode, bool>(nameof(IsStoreEnabled),
        o => o.IsStoreEnabled,
        (o, v) => o.IsStoreEnabled = v,
        defaultBindingMode: BindingMode.OneWay);
    RelayCommand<BreakpointMode> SetModeCommand { get; }
    BreakpointMode mode;
    bool isExecEnabled;
    bool isLoadEnabled;
    bool isStoreEnabled;
    public BreakpointExecutionMode()
    {
        SetModeCommand = new RelayCommand<BreakpointMode>(m => Mode = m);
        InitializeComponent();
        UpdateClasses();
    }
    void UpdateClasses()
    {
        const string Selected = "selected";
        ExecButton.Classes.Set(Selected, Mode == BreakpointMode.Exec);
        LoadButton.Classes.Set(Selected, Mode == BreakpointMode.Load);
        StoreButton.Classes.Set(Selected, Mode == BreakpointMode.Store);
    }
    public BreakpointMode Mode
    {
        get => mode;
        set => SetAndRaise(ModeProperty, ref mode, value);
    }
    public bool IsExecEnabled
    {
        get => isExecEnabled;
        set => SetAndRaise(IsExecEnabledProperty, ref isExecEnabled, value);
    }
    public bool IsLoadEnabled
    {
        get => isLoadEnabled;
        set => SetAndRaise(IsLoadEnabledProperty, ref isLoadEnabled, value);
    }
    public bool IsStoreEnabled
    {
        get => isStoreEnabled;
        set => SetAndRaise(IsStoreEnabledProperty, ref isStoreEnabled, value);
    }
}
