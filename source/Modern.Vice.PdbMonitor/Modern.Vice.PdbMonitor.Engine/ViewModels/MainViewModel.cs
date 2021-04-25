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
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;
using ViceBridgeCommand = Righthand.ViceMonitor.Bridge.Commands;

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
        readonly IViceBridge viceBridge;
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
        public RelayCommand RunCommand { get; }
        public RelayCommand StopCommand { get; }
        public Func<string?, CancellationToken, Task<string?>>? ShowCreateProjectFileDialogAsync { get; set; }
        public Func<string?, CancellationToken, Task<string?>>? ShowOpenProjectFileDialogAsync { get; set; }
        public Action? CloseApp { get; set; }
        public bool IsShowingSettings => OverlayContent is SettingsViewModel;
        public bool IsShowingProject => OverlayContent is ProjectViewModel;
        public bool IsShowingErrors { get; set; }
        public bool IsBusy { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsOverlayVisible => OverlayContent is not null;
        public bool IsViceConnected { get; private set; }
        public ErrorMessagesViewModel ErrorMessagesViewModel { get; }
        public ScopedViewModel Content { get; private set; } = default!;
        public ScopedViewModel? OverlayContent { get; private set; }
        Process? viceProcess;
        CancellationTokenSource? startDebuggingCts;
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
            ISettingsManager settingsManager, ErrorMessagesViewModel errorMessagesViewModel, IServiceScope scope, IViceBridge viceBridge)
        {
            this.logger = logger;
            this.acmePdbParser = acmePdbParser;
            this.globals = globals;
            this.dispatcher = dispatcher;
            this.settingsManager = settingsManager;
            this.scope = scope;
            this.viceBridge = viceBridge;
            closeOverlaySubscription = dispatcher.Subscribe<CloseOverlayMessage>(CloseOverlay);
            ErrorMessagesViewModel = errorMessagesViewModel;
            ShowSettingsCommand = new RelayCommand(ShowSettings, () => !IsShowingSettings);
            ShowProjectCommand = new(ShowProject, () => !IsShowingProject && IsProjectOpen);
            TestCommand = new RelayCommand(Test, () => !IsBusy);
            CreateProjectCommand = new RelayCommand(CreateProject, () => !IsBusy && !IsRunning);
            OpenProjectFromPathCommand = new RelayCommand<string>(OpenProjectFromPath, _ => !IsBusy && !IsRunning);
            OpenProjectCommand = new RelayCommand(OpenProject, () => !IsBusy && !IsRunning);
            globals.PropertyChanged += Globals_PropertyChanged;
            CloseProjectCommand = new RelayCommand(CloseProject, () => IsProjectOpen && !IsRunning);
            ExitCommand = new RelayCommand(() => CloseApp?.Invoke());
            ToggleErrorsVisibilityCommand = new RelayCommand(() => IsShowingErrors = !IsShowingErrors);
            RunCommand = new(StartDebuggingAsync, () => IsProjectOpen && !IsRunning);
            StopCommand = new(StopDebugging, () => IsRunning);
            if (!Directory.Exists(globals.Settings.VicePath))
            {
                SwitchContent<SettingsViewModel>();
            }
            else
            {
                SwitchContent<DebuggerViewModel>();
                // by default opens most recent project
                if (globals.Settings.RecentProjects.Count > 0)
                {
                    OpenProjectFromPath(globals.Settings.RecentProjects[0]);
                }
            }
            viceBridge.ConnectedChanged += ViceBridge_ConnectedChanged;
            viceBridge.Start();
        }

        void ViceBridge_ConnectedChanged(object? sender, ConnectedChangedEventArgs e)
        {
            IsViceConnected = e.IsConnected;
        }

        internal void StopDebugging()
        {
            _ = ClearAfterDebugging();
        }
        internal async void StartDebuggingAsync()
        {
            const string Title = "Start debugging";
            viceProcess = StartVice();
            if (viceProcess is null)
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, Title, "Failed to start debugging"));
            }
            IsBusy = true;
            IsRunning = true;
            try
            {
                startDebuggingCts = new CancellationTokenSource();
                if (!viceBridge.IsConnected)
                {
                    await viceBridge.WaitForConnectionStatusChangeAsync(startDebuggingCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                await ClearAfterDebugging();
            }
            finally
            {
                IsBusy = false;
            }
        }
        internal async Task ClearAfterDebugging()
        {
            logger.LogDebug("Cleaning after debugging");
            if (viceProcess is not null)
            {
                if (viceBridge.IsConnected)
                {
                    logger.LogDebug("VICE process running and bridge is connected, will try Quit command");
                    var quitCommand = new ViceBridgeCommand.QuitCommand();
                    viceBridge.EnqueueCommand(quitCommand);
                    (bool success, var result) = await quitCommand.Response.AwaitWithTimeout(TimeSpan.FromSeconds(10));
                    bool isQuitSuccess = true;
                    if (!success)
                    {
                        logger.LogDebug("Timeout while waiting for VICE response to Quit command");
                        isQuitSuccess = false;
                    }
                    else
                    {
                        if (result!.ErrorCode != ViceBridgeCommand.ErrorCode.OK)
                        {
                            logger.LogDebug("VICE returned {ErrorCode} to Quit command", result!.ErrorCode);
                            isQuitSuccess = false;
                        }
                    }
                    if (!isQuitSuccess)
                    {
                        viceProcess.Kill();
                    }
                    bool waitForKillSuccess = viceProcess.WaitForExit(5000);
                    if (!waitForKillSuccess)
                    {
                        logger.LogWarning("Couldn't kill VICE process");
                    }
                    viceProcess.Dispose();
                }
            }
            IsRunning = false;
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
        internal void SwitchContent<T>()
            where T: ScopedViewModel
        {
            Content?.Dispose();
            Content = scope.ServiceProvider.CreateScopedContent<T>();
        }
        internal void SwitchOverlayContent<T>()
            where T : ScopedViewModel
        {
            OverlayContent?.Dispose();
            OverlayContent = scope.ServiceProvider.CreateScopedContent<T>();
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
        internal Process? StartVice()
        {
            if (!string.IsNullOrWhiteSpace(globals.Settings.VicePath))
            {
                string path = Path.Combine(globals.Settings.VicePath, "bin", "x64dtv.exe");
                try
                {
                    return Process.Start(path, $"-binarymonitor -autostartprgmode 1 {globals.FullPrgPath}");
                }
                catch (Exception ex)
                {
                    dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Warning, "Starting VICE", ex.Message));
                    return null;
                }
            }
            else
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Warning, "Starting VICE", "VICE path is not set in settings"));
                return null;
            }
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
                    RunCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(OverlayContent):
                    ShowSettingsCommand.RaiseCanExecuteChanged();
                    ShowProjectCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(IsRunning):
                    CloseProjectCommand.RaiseCanExecuteChanged();
                    CreateProjectCommand.RaiseCanExecuteChanged();
                    OpenProjectCommand.RaiseCanExecuteChanged();
                    OpenProjectFromPathCommand.RaiseCanExecuteChanged();
                    RunCommand.RaiseCanExecuteChanged();
                    StopCommand.RaiseCanExecuteChanged();
                    break; 
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                viceBridge.ConnectedChanged -= ViceBridge_ConnectedChanged;
            }
            base.Dispose(disposing);
        }
    }
}
