using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views
{
    public class Project : UserControl
    {
        public Project()
        {
            InitializeComponent();
        }

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        async void OpenPrgFile(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open PRG file",
                AllowMultiple = false,
            };
            dialog.Filters.Add(new FileDialogFilter { Name = "ACME Compiled .prg", Extensions = { "prg" } });
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var result = await dialog.ShowAsync(desktop.MainWindow);
                if (result?.Length > 0)
                {
                    var viewModel = (ProjectViewModel)DataContext!;
                    viewModel.AssignPrgFulPath(result[0]);
                }
            }
        }
    }
}
