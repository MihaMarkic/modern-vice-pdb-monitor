using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Modern.Vice.PdbMonitor.Views
{
    public partial class Breakpoints : UserControl
    {
        public Breakpoints()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
