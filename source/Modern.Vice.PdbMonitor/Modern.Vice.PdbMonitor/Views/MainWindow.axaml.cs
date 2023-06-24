using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine;
using Modern.Vice.PdbMonitor.Engine.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views;

partial class MainWindow : Window
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
        viewModel.ShowModalDialog = ShowModalDialog;
    }
    internal void ShowModalDialog(ShowModalDialogMessageCore message)
    {
        var dialog = new ModalDialogWindow
        {
            // TODO make generic
            DataContext = message,
            MinWidth = 500,
            Height = 350,
            MinHeight = 350,
            SizeToContent = SizeToContent.Width,
        };
        dialog.ShowDialog(this);
    }
    internal async Task<string?> ShowOpenProjectFileDialogAsync(OpenFileDialogModel model,
        CancellationToken ct)
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow!.StorageProvider;
            var options = new FilePickerOpenOptions
            {
                Title = "Open project",
                AllowMultiple = false,
                FileTypeFilter = new FilePickerFileType[]
                {
                    new (model.Name)
                    {
                        Patterns = new []{ model.Extension }
                    }
                },
            };
            if (model.InitialDirectory is not null)
            {
                options.SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(model.InitialDirectory);
            }
            var result = await storageProvider.OpenFilePickerAsync(options);
            if (result?.Count == 1)
            {
                return result[0].Path.LocalPath;
            }
        }
        return null;
    }
    internal async Task<string?> ShowCreateProjectFileDialogAsync(OpenFileDialogModel model, CancellationToken ct)
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow!.StorageProvider;
            var options = new FilePickerSaveOptions
            {
                Title = "Create project",
                DefaultExtension = model.Extension,
                FileTypeChoices = new FilePickerFileType[]
                {
                    new (model.Name)
                    {
                        Patterns = new []{ model.Extension }
                    }
                },
            };
            if (model.InitialDirectory is not null)
            {
                options.SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(model.InitialDirectory);
            }
            var result = await storageProvider.SaveFilePickerAsync(options);
            return result?.Path.LocalPath;
        }
        return null;
    }

    public MainViewModel ViewModel => (MainViewModel)DataContext!;

    protected override void OnClosed(EventArgs e)
    {
        scope.Dispose();
        Bootstrap.Close();
        base.OnClosed(e);
    }
}
