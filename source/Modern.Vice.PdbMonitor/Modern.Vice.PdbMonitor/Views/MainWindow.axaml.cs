using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views
{
    public class MainWindow : Window
    {
        readonly IServiceScope scope;
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            scope = IoC.Host.Services.CreateScope();
            Bootstrap.Init(scope);
            var viewModel = scope.ServiceProvider.GetService<MainViewModel>()!;
            DataContext = viewModel;
        }

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        public MainViewModel ViewModel => (MainViewModel)DataContext!;
        async void CreateProject(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "PRG file selection",
                AllowMultiple = false,
            };
            dialog.Filters.Add(new FileDialogFilter { Name = "ACME .prg", Extensions = { "prg" } });

            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var result = await dialog.ShowAsync(desktop.MainWindow);
                if (result.Length == 1)
                {
                    ViewModel.CreateProject(result[0]);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Bootstrap.Close();
            scope.Dispose();
            base.OnClosed(e);
        }
    }
}
