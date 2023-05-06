using Avalonia.Controls;

namespace Modern.Vice.PdbMonitor.Views;
public partial class TraceOutput : UserControl
{
    public TraceOutput()
    {
        InitializeComponent();
        Output.CaretIndex = int.MaxValue;
        Follow.Tapped += Output_Tapped;
    }

    void Output_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        Output.CaretIndex = int.MaxValue;
    }
}
