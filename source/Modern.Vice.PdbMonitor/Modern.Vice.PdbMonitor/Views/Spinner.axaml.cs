using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace Modern.Vice.PdbMonitor.Views;

public class Spinner : UserControl
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
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
