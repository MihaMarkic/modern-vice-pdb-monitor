using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
        var dialog = new OpenFileDialog
        {
            Title = model.Title,
            AllowMultiple = false,
        };
        if (model.InitialDirectory is not null)
        {
            dialog.Directory = model.InitialDirectory;
        }
        if (dialog.Filters is null)
        {
            dialog.Filters = new();
        }
        dialog.Filters.Add(new FileDialogFilter { Name = model.Name, Extensions = { model.Extension } });
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowAsync(desktop.MainWindow);
            if (result?.Length > 0)
            {
                var viewModel = (ProjectViewModel)DataContext!;
                return result[0];
            }
        }
        return null;
    }
}
