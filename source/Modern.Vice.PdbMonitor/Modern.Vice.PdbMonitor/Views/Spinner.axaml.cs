using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace Modern.Vice.PdbMonitor.Views;

partial class Spinner : UserControl
{
    public static readonly DirectProperty<Spinner, bool> IsActiveProperty = AvaloniaProperty.RegisterDirect<Spinner, bool>(nameof(IsActive),
        o => o.IsActive, (o ,v) => o.IsActive = v, defaultBindingMode: BindingMode.OneWay);
    bool isActive;
    public Spinner()
    {
        InitializeComponent();
    }
    public bool IsActive
    {
        get => isActive;
        set => SetAndRaise(IsActiveProperty, ref isActive, value);
    }
}
