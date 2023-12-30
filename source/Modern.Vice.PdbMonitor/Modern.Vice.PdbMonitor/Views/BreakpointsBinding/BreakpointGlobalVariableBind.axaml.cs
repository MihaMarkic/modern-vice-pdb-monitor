using Avalonia;
using Avalonia.Controls;
using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Views.BreakpointsBinding;

public partial class BreakpointGlobalVariableBind : UserControl
{
    public static DirectProperty<BreakpointGlobalVariableBind, ImmutableArray<PdbVariable>> GlobalVariablesProperty =
        AvaloniaProperty.RegisterDirect<BreakpointGlobalVariableBind, ImmutableArray<PdbVariable>>(
            nameof(GlobalVariables), 
            o => o.GlobalVariables, 
            (o, gv) => o.GlobalVariables = gv, 
            unsetValue: ImmutableArray<PdbVariable>.Empty,
            defaultBindingMode: Avalonia.Data.BindingMode.OneWay);
    public static DirectProperty<BreakpointGlobalVariableBind, PdbVariable?> VariableProperty =
        AvaloniaProperty.RegisterDirect<BreakpointGlobalVariableBind, PdbVariable?>(
            nameof(Variable),
            o => o.Variable,
            (o, v) => o.Variable = v,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    ImmutableArray<PdbVariable> globalVariables = ImmutableArray<PdbVariable>.Empty;
    PdbVariable? variable;
    public BreakpointGlobalVariableBind()
    {
        InitializeComponent();
    }
    public ImmutableArray<PdbVariable> GlobalVariables
    {
        get => globalVariables;
        set => SetAndRaise(GlobalVariablesProperty, ref globalVariables, value);
    }
    public PdbVariable? Variable
    {
        get => variable;
        set => SetAndRaise(VariableProperty, ref variable, value);
    }
}
