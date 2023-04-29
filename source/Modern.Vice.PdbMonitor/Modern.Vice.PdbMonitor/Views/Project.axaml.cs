using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Modern.Vice.PdbMonitor.Engine.Common;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views;

partial class Project : UserControl
{
    ProjectViewModel? viewModel;
    public Project()
    {
        InitializeComponent();
    }
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not null)
        {
            var viewModel = (ProjectViewModel)DataContext!;
            viewModel.ShowOpenDebugDataFileDialogAsync = ShowOpenDebugDataFileDialogAsync;
        }
        else if (viewModel is not null)
        {
            viewModel.ShowOpenDebugDataFileDialogAsync = null;
            viewModel = null;
        }
    }
    async Task<string?> ShowOpenDebugDataFileDialogAsync(DebugFileOpenDialogModel model, CancellationToken ct)
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow!.StorageProvider;
            var options = new FilePickerOpenOptions
            {
                Title = model.Title,
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
}
