using Avalonia.Controls;
using Modern.Vice.PdbMonitor.Controls.ValueEditors;

namespace Modern.Vice.PdbMonitor.Views;
public partial class Variables : UserControl
{
    public Variables()
    {
        InitializeComponent();
        Grid.CellEditEnding += Grid_CellEditEnding;
    }

    void Grid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit && e.EditingElement is VariableEditor variableEditor)
        {
            _ = variableEditor.SaveValueAsync();
        }
    }
}
