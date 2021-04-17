using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Modern.Vice.PdbMonitor.Views
{
    public class ErrorMessages : UserControl
    {
        public ErrorMessages()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
