using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class MainViewModel: NotifiableObject
    {
        readonly ILogger<MainViewModel> logger;
        readonly IAcmePdbParser acmePdbParser;
        readonly Globals globals;
        readonly IDispatcher dispatcher;
        readonly ISettingsManager settingsManager;
        readonly IServiceScope scope;
        public string AppName => "Modern VICE PDB Monitor";
        readonly Subscription closeOverlaySubscription;
        public Project? Project => globals.Project;
        public bool IsProjectOpen => Project is not null;
        public ObservableCollection<string> RecentProjects => globals.Settings.RecentProjects;
        public RelayCommand ShowSettingsCommand { get; }
        public RelayCommand ShowProjectCommand { get; }
        public RelayCommand TestCommand { get; }
        public RelayCommand CreateProjectCommand { get; }
        public RelayCommand<string> OpenProjectFromPathCommand { get; }
        public RelayCommand OpenProjectCommand { get; }
        public RelayCommand CloseProjectCommand { get; }
        public RelayCommand ExitCommand { get; }
        public RelayCommand ToggleErrorsVisibilityCommand { get; }
        public Func<string?, CancellationToken, Task<string?>>? ShowCreateProjectFileDialogAsync { get; set; }
        public Func<string?, CancellationToken, Task<string?>>? ShowOpenProjectFileDialogAsync { get; set; }
        public Action? CloseApp { get; set; }
        public bool IsShowingSettings => OverlayContent is SettingsViewModel;
        public bool IsShowingProject => OverlayContent is ProjectViewModel;
        public bool IsShowingErrors { get; set; }
        public bool IsBusy { get; private set; }
        public bool IsOverlayVisible => OverlayContent is not null;
        public ErrorMessagesViewModel ErrorMessagesViewModel { get; }
        public ContentViewModel Content { get; private set; } = default!;
        public ContentViewModel? OverlayContent { get; private set; }
        public string Caption
        {
            get
            {
                if (Project is null)
                {
                    return AppName;
                }
                else
                {
                    return $"{AppName} - {globals.Settings.RecentProjects[0]}";
                }
            }
        }
        public MainViewModel(ILogger<MainViewModel> logger, IAcmePdbParser acmePdbParser, Globals globals, IDispatcher dispatcher,
            ISettingsManager settingsManager, ErrorMessagesViewModel errorMessagesViewModel, IServiceScope scope)
        {
            this.logger = logger;
            this.acmePdbParser = acmePdbParser;
            this.globals = globals;
            this.dispatcher = dispatcher;
            this.settingsManager = settingsManager;
            this.scope = scope;
            closeOverlaySubscription = dispatcher.Subscribe<CloseOverlayMessage>(CloseOverlay);
            ErrorMessagesViewModel = errorMessagesViewModel;
            ShowSettingsCommand = new RelayCommand(ShowSettings, () => !IsShowingSettings);
            ShowProjectCommand = new(ShowProject, () => !IsShowingProject && IsProjectOpen);
            TestCommand = new RelayCommand(Test, () => !IsBusy);
            CreateProjectCommand = new RelayCommand(CreateProject, () => !IsBusy);
            OpenProjectFromPathCommand = new RelayCommand<string>(OpenProjectFromPath, _ => !IsBusy);
            OpenProjectCommand = new RelayCommand(OpenProject, () => !IsBusy);
            globals.PropertyChanged += Globals_PropertyChanged;
            CloseProjectCommand = new RelayCommand(CloseProject, () => Project is not null);
            ExitCommand = new RelayCommand(() => CloseApp?.Invoke());
            ToggleErrorsVisibilityCommand = new RelayCommand(() => IsShowingErrors = !IsShowingErrors);
            if (!Directory.Exists(globals.Settings.VicePath))
            {
                SwitchContent<SettingsViewModel>();
            }
            else
            {
                SwitchContent<DebuggerViewModel>();
            }
        }
        internal void CloseOverlay(object sender, CloseOverlayMessage message)
        {
            OverlayContent?.Dispose();
            OverlayContent = null;
        }
        internal void ShowSettings()
        {
            if (!IsShowingSettings)
            {
                SwitchOverlayContent<SettingsViewModel>();
            }
        }
        internal void ShowProject()
        {
            if (!IsShowingProject)
            {
                SwitchOverlayContent<ProjectViewModel>();
            }
        }
        internal T CreateContent<T>()
            where T : ContentViewModel
        {
            var contentScope = scope.ServiceProvider.CreateScope();
            T viewModel = contentScope.ServiceProvider.GetService<T>() ?? throw new Exception($"Failed creating {typeof(T).Name} ViewModel");
            viewModel.AssignScope(contentScope);
            return viewModel;
        }
        internal void SwitchContent<T>()
            where T: ContentViewModel
        {
            Content?.Dispose();
            Content = CreateContent<T>();
        }
        internal void SwitchOverlayContent<T>()
            where T : ContentViewModel
        {
            OverlayContent?.Dispose();
            OverlayContent = CreateContent<T>();
        }
        void Globals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Globals.Project):
                    OnPropertyChanged(nameof(Project));
                    OnPropertyChanged(nameof(Caption));
                    break;
            }
        }
        internal void CloseProject()
        {
            globals.Project = null;
            globals.Pdb = null;
        }
        void Test()
        {
            if (!string.IsNullOrWhiteSpace(globals.Settings.VicePath))
            {
                string path = Path.Combine(globals.Settings.VicePath, "bin", "x64dtv.exe");
                var proc = Process.Start(path, "-binarymonitor");
            }
        }
        internal void OpenProjectFromPath(string? path) => _ = OpenProjectFromPathInternal(path);
        internal async Task<bool> OpenProjectFromPathInternal(string? path)
        {
            const string ErrorTitle = "Failed opening project";
            if (path is null)
            {
                return false;
            }
            IsBusy = true;
            try
            {
                if (!File.Exists(path))
                {
                    dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, ErrorTitle, $"Project file {path} does not exist."));
                    return false;
                    //globals.Pdb = await ParsePdbAsync(GetPdbPath(prgPath));
                }
                var project = settingsManager.Load<Project>(path)!;
                if (project.PrgPath is not null)
                {
                    string pdbPath = GetPdbPath(project.PrgPath);
                    globals.Pdb = await ParsePdbAsync(pdbPath);
                }
                globals.Project = project;
                globals.Settings.AddRecentProject(path);
            }
            catch (Exception ex)
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, ErrorTitle, ex.Message));
            }
            finally
            {
                IsBusy = false;
            }
            return false;
        }
        public async void OpenProject()
        {
            if (ShowOpenProjectFileDialogAsync is not null)
            {
                string? projectPath = await ShowOpenProjectFileDialogAsync(globals.Settings.LastAccessedDirectory, CancellationToken.None);
                if (!string.IsNullOrWhiteSpace(projectPath))
                {
                    _ = OpenProjectFromPathInternal(projectPath);
                }
            }
        }
        public async void CreateProject()
        {
            if (ShowCreateProjectFileDialogAsync is not null)
            {
                string? projectPath = await ShowCreateProjectFileDialogAsync(globals.Settings.LastAccessedDirectory, CancellationToken.None);
                if (!string.IsNullOrWhiteSpace(projectPath))
                {
                    bool success = CreateProject(projectPath);
                    if (success)
                    {
                        globals.Settings.AddRecentProject(projectPath);
                    }
                }
            }
        }

        internal bool CreateProject(string projectPath)
        {
            if (File.Exists(projectPath))
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Failed creating project", $"Project file {projectPath} already exists."));
                return false;
                //globals.Pdb = await ParsePdbAsync(GetPdbPath(prgPath));
            }
            else
            {
                try
                {
                    var project = new Project();
                    settingsManager.Save(project, projectPath, false);
                    globals.Project = project;
                    return true;
                }
                catch (Exception ex)
                {
                    dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Failed creating project", ex.Message));
                }
            }
            return false;
        }

        internal string GetPdbPath(string prgPath) => Path.Combine(globals.ProjectDirectory!, Path.GetDirectoryName(prgPath) ?? "", $"{Path.GetFileNameWithoutExtension(prgPath)}.pdb");

        internal async Task<AcmePdb?> ParsePdbAsync(string pdbPath)
        {
            if (!File.Exists(pdbPath))
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Failed parsing PDB", $"PDB file {pdbPath} does not exist"));
                return null;
            }
            IsBusy = true;
            try
            {
                var result = await Task.Run(() => acmePdbParser.ParseAsync(pdbPath));
                if (result.Errors.Length > 0)
                {
                    string errorMessage = string.Join('\n', result.Errors.Select(e => e.ErrorText));
                    dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Failed parsing PDB", errorMessage));
                    return null;
                }
                else
                {
                    return result.AcmePdb;
                }
            }
            catch (Exception ex)
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Failed parsing PDB", ex.Message));
                return null;
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected override void OnPropertyChanged([CallerMemberName]string name = default!)
        {
            base.OnPropertyChanged(name);
            switch (name)
            {
                case nameof(IsBusy):
                    CreateProjectCommand.RaiseCanExecuteChanged();
                    OpenProjectFromPathCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(Project):
                    CloseProjectCommand.RaiseCanExecuteChanged();
                    ShowProjectCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(OverlayContent):
                    ShowSettingsCommand.RaiseCanExecuteChanged();
                    ShowProjectCommand.RaiseCanExecuteChanged();
                    break;
            }
        }
    }
}
