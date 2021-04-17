using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using Righthand.MessageBus;

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
            viewModel.ShowCreateProjectFileDialogAsync = ShowCreateProjectFileDialogAsync;
            viewModel.ShowOpenProjectFileDialogAsync = ShowOpenProjectFileDialogAsync;
            viewModel.CloseApp = Close;
            var dispatcher = scope.ServiceProvider.GetService<Righthand.MessageBus.IDispatcher>()!;
        }

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        internal async Task<string?> ShowOpenProjectFileDialogAsync(string? initialDirectory, CancellationToken ct)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open project",
                AllowMultiple = false,
            };
            if (initialDirectory is not null)
            {
                dialog.Directory = initialDirectory;
            }
            dialog.Filters.Add(new FileDialogFilter { Name = "Modern ACME PDB Debugger .mapd", Extensions = { "mapd" } });
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var result = await dialog.ShowAsync(desktop.MainWindow);
                if (result.Length == 1)
                {
                    return result[0];
                }
            }
            return null;
        }
        internal async Task<string?> ShowCreateProjectFileDialogAsync(string? initialDirectory, CancellationToken ct)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Create project",
            };
            if (initialDirectory is not null)
            {
                dialog.Directory = initialDirectory;
            }
            dialog.Filters.Add(new FileDialogFilter { Name = "Modern ACME PDB Debugger .mapd", Extensions = { "mapd" } });
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var result = await dialog.ShowAsync(desktop.MainWindow);
                return result;
            }
            return null;
        }

        public MainViewModel ViewModel => (MainViewModel)DataContext!;
        //async void CreateProject(object sender, RoutedEventArgs e)
        //{
        //    var dialog = new OpenFileDialog
        //    {
        //        Title = "PRG file selection",
        //        AllowMultiple = false,
        //    };
        //    dialog.Filters.Add(new FileDialogFilter { Name = "ACME .prg", Extensions = { "prg" } });

        //    if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        //    {
        //        var result = await dialog.ShowAsync(desktop.MainWindow);
        //        if (result.Length == 1)
        //        {
        //            ViewModel.CreateProject result[0]);
        //        }
        //    }
        //}

        protected override void OnClosed(EventArgs e)
        {
            Bootstrap.Close();
            scope.Dispose();
            base.OnClosed(e);
        }
    }
}
