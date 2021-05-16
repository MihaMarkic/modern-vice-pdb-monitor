using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Modern.Vice.PdbMonitor.Views
{
    public partial class Flags : UserControl
    {
        byte? value;
        public static readonly DirectProperty<Flags, byte?> ValueProperty =
            AvaloniaProperty.RegisterDirect<Flags, byte?>(nameof(Value), o => o.Value, (o, v) => o.Value = v);
        public Flags()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        public byte? Value
        {
            get => value;
            set => SetAndRaise(ValueProperty, ref this.value, value);
        }
    }
}
