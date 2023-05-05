using Avalonia;
using Avalonia.Controls;

namespace Modern.Vice.PdbMonitor.Views;

public partial class Flags : UserControl
{
    byte? value;
    public static readonly DirectProperty<Flags, byte?> ValueProperty =
        AvaloniaProperty.RegisterDirect<Flags, byte?>(nameof(Value), o => o.Value, (o, v) => o.Value = v);
    public Flags()
    {
        InitializeComponent();
    }
    public byte? Value
    {
        get => value;
        set => SetAndRaise(ValueProperty, ref this.value, value);
    }
}
