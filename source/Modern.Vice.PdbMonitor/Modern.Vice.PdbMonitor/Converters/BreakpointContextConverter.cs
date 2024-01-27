using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Converters;
public class BreakpointContextConverter : AvaloniaObject, IMultiValueConverter
{
    public static readonly DirectProperty<BreakpointContextConverter, DataGridColumn?> BindingColumnProperty =
        AvaloniaProperty.RegisterDirect<BreakpointContextConverter, DataGridColumn?>(nameof(BindingColumn),
        o => o.BindingColumn,
        (o, v) => o.BindingColumn = v,
        defaultBindingMode: BindingMode.TwoWay);
    private DataGridColumn? bindingColumn;
    public DataGridColumn? BindingColumn 
    { 
        get => bindingColumn;
        set => SetAndRaise(BindingColumnProperty, ref bindingColumn, value);
    }
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 2 
            && values[0] is BreakpointViewModel viewModel
            && values[1] is DataGridColumn column)
        {
            var mode = ReferenceEquals(column, BindingColumn) 
                ? BreakpointsViewModel.BreakPointContextColumn.Binding
                : BreakpointsViewModel.BreakPointContextColumn.Other;
            return new BreakpointsViewModel.BreakPointContext(viewModel, mode);
        }
        return default;
    }
}
