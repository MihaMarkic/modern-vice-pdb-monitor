using Avalonia.Controls;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views;

partial class Breakpoints : UserControl
{
    public Breakpoints()
    {
        InitializeComponent();
        Grid.DoubleTapped += Grid_DoubleTapped;
    }
    private BreakpointsViewModel ViewModel => (BreakpointsViewModel)DataContext!;
    private void Grid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (ViewModel is not null)
        {
            var command = ViewModel.BreakPointContextCommand;
            var mode = ReferenceEquals(Grid.CurrentColumn, Grid.Columns[6])
                ? BreakpointsViewModel.BreakPointContextColumn.Binding
                : BreakpointsViewModel.BreakPointContextColumn.Other;
            var parameter = new BreakpointsViewModel.BreakPointContext((BreakpointViewModel)Grid.SelectedItem, mode);
            if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }
    }
}
