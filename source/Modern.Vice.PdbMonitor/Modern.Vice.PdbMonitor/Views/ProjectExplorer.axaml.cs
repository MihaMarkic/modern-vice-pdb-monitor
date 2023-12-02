using Avalonia.Controls;

namespace Modern.Vice.PdbMonitor.Views;

partial class ProjectExplorer : UserControl
{
    public ProjectExplorer()
    {
        InitializeComponent();
    }
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
    }
}
