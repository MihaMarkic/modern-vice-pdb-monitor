using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views
{
    public class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
        }

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        async void OpenViceDirectory(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "VICE directory selection"
            };
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var result = await dialog.ShowAsync(desktop.MainWindow);
                if (result is not null)
                {
                    var viewModel = (SettingsViewModel)DataContext!;
                    viewModel.Settings.VicePath = result;
                }
            }
        }
    }
}
