using Avalonia;
using Avalonia.Controls;

namespace Modern.Vice.PdbMonitor.Views;

public partial class Register : UserControl
{
    object? value;
    string? caption;
    public static readonly DirectProperty<Register, object?> ValueProperty =
        AvaloniaProperty.RegisterDirect<Register, object?>(nameof(Value), o => o.Value, (o, v) => o.Value = v);
    public static readonly DirectProperty<Register, string?> CaptionProperty =
        AvaloniaProperty.RegisterDirect<Register, string?>(nameof(Caption), o => o.Caption, (o, v) => o.Caption = v);
    public Register()
    {
        InitializeComponent();
    }
    public object? Value
    {
        get => value;
        set => SetAndRaise(ValueProperty, ref this.value, value);
    }
    public string? Caption
    {
        get => caption;
        set => SetAndRaise(CaptionProperty, ref caption, value);
    }
}
