using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Modern.Vice.PdbMonitor.Engine.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Righthand.MessageBus;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Core;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class ProjectViewModel : OverlayContentViewModel
{
    readonly Globals globals;
    readonly ISettingsManager settingsManager;
    readonly IProjectPrgFileWatcher projectPrgFileWatcher;
    readonly ExecutionStatusViewModel executionStatusViewModel;
    readonly TaskFactory uiFactory;
    readonly IProjectFactory projectFactory;
    readonly CommandsManager commandsManager;
    public Project Project => globals.Project!;
    public string ProjectFile => globals.Project!.File!;
    public string? PrgPath
    {
        get => Project.PrgPath;
        set => Project.PrgPath = value;
    }
    public DebugAutoStartMode AutoStartMode
    {
        get => Project.AutoStartMode;
        set => Project.AutoStartMode = value;
    }
    public string StopAtLabel
    {
        get => Project.StopAtLabel;
        set => Project.StopAtLabel = value;
    }
    /// <summary>
    /// Contains full directory to PRG file
    /// </summary>
    public string? PrgDirectory => Project.FullPrgPath is not null ? Path.GetDirectoryName(Project.FullPrgPath) : null;
    public CompilerType CompilerType => Project.CompilerType;
    public bool IsStartingDebugging => executionStatusViewModel.IsDebugging;
    public bool IsDebugging => executionStatusViewModel.IsDebugging;
    public bool IsDebuggingPaused => executionStatusViewModel.IsDebuggingPaused;
    public bool IsEditable => !IsStartingDebugging && !IsDebugging;
    public ImmutableArray<string> AllLabels =>
        ImmutableArray<string>.Empty.Add(Project.StopAtLabelNone).AddRange(globals.Project!.DebugSymbols?.Labels.Keys.ToImmutableArray() ?? ImmutableArray<string>.Empty);
    public RelayCommand OpenDebugDataFileCommand { get; }
    public Func<DebugFileOpenDialogModel, CancellationToken, Task<string?>>? ShowOpenDebugDataFileDialogAsync { get; set; }
    public ProjectViewModel(Globals globals, ISettingsManager settingsManager, IDispatcher dispatcher,
        IProjectPrgFileWatcher projectPrgFileWatcher, ExecutionStatusViewModel executionStatusViewModel, 
        IProjectFactory projectFactory) : base(dispatcher)
    {
        this.globals = globals;
        this.settingsManager = settingsManager;
        this.projectPrgFileWatcher = projectPrgFileWatcher;
        this.executionStatusViewModel = executionStatusViewModel;
        executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
        uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        this.projectFactory = projectFactory;
        commandsManager = new CommandsManager(this, uiFactory);
        OpenDebugDataFileCommand = commandsManager.CreateRelayCommand(OpenDebugDataFileAsync, () => IsEditable);
        this.projectFactory = projectFactory;
    }

    void ExecutionStatusViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ExecutionStatusViewModel.IsStartingDebugging):
                OnPropertyChanged(nameof(IsStartingDebugging));
                break;
            case nameof(ExecutionStatusViewModel.IsDebugging):
                OnPropertyChanged(nameof(IsDebugging));
                break;
            case nameof(ExecutionStatusViewModel.IsDebuggingPaused):
                OnPropertyChanged(nameof(IsDebuggingPaused));
                break;
        }
    }
    public async void OpenDebugDataFileAsync()
    {
        if (ShowOpenDebugDataFileDialogAsync is not null)
        {
            var model = projectFactory.GetDebugFileOpenDialogModel(PrgDirectory, CompilerType);
            string? debugFilePath = await ShowOpenDebugDataFileDialogAsync(model, CancellationToken.None);
            if (!string.IsNullOrWhiteSpace(debugFilePath))
            {
                AssignPrgFullPath(debugFilePath);
            }
        }
    }
    public void AssignPrgFullPath(string value)
    {
        try
        {
            var prgPath = new Uri(value);
            var projectDirectory = new Uri(Path.Combine($"{globals.Project!.Directory}{Path.DirectorySeparatorChar}"));
            PrgPath = projectDirectory.MakeRelativeUri(prgPath).ToString();
            dispatcher.Dispatch(new PrgFilePathChangedMessage());
            projectPrgFileWatcher.Start(globals.Project!.Directory!, value);
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Assigning PRG (.o)", ex.Message));
        }
    }
    public bool IsPrgPathValid
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(PrgPath))
            {
                string fullPath = Path.Combine(globals.Project!.Directory!, PrgPath);
                return File.Exists(fullPath);
            }
            return false;
        }
    }
    protected override void OnPropertyChanged(string name = null!)
    {
        switch (name)
        {
            case nameof(PrgPath):
            case nameof(AutoStartMode):
            case nameof(StopAtLabel):
                try
                {
                    settingsManager.Save(Project.ToConfiguration(), globals.Settings.RecentProjects[0], createDirectory: false);
                }
                catch (Exception ex)
                {
                    dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Save project", ex.Message));
                }
                break;
        }
        base.OnPropertyChanged(name);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            executionStatusViewModel.PropertyChanged -= ExecutionStatusViewModel_PropertyChanged;
        }
        base.Dispose(disposing);
    }
}
