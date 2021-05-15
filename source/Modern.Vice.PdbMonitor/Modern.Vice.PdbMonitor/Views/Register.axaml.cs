using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Modern.Vice.PdbMonitor.Views
{
    public partial class Register : UserControl
    {
        object? value;
        string? caption;
        //int digits = 2;
        public static readonly DirectProperty<Register, object?> ValueProperty =
            AvaloniaProperty.RegisterDirect<Register, object?>(nameof(Value), o => o.Value, (o, v) => o.Value = v);
        //public static readonly DirectProperty<Register, int> DigitsProperty =
        //    AvaloniaProperty.RegisterDirect<Register, int>(nameof(Digits), o => o.Digits, (o, v) => o.Digits = v);
        public static readonly DirectProperty<Register, string?> CaptionProperty =
            AvaloniaProperty.RegisterDirect<Register, string?>(nameof(Caption), o => o.Caption, (o, v) => o.Caption = v);
        public Register()
        {
            InitializeComponent();
        }
        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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
        //public int Digits
        //{
        //    get => digits;
        //    set => SetAndRaise(DigitsProperty, ref digits, value);
        //}
    }
}
