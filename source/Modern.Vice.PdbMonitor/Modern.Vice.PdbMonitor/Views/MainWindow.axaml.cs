using System;
using Avalonia;
using Avalonia.Controls;
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

        protected override void OnClosed(EventArgs e)
        {
            Bootstrap.Close();
            scope.Dispose();
            base.OnClosed(e);
        }
    }
}
