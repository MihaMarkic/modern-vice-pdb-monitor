using System;
using System.Collections.Immutable;
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
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;
using Righthand.ViceMonitor.Bridge.Shared;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class MainViewModel : NotifiableObject
    {
        readonly ILogger<MainViewModel> logger;
        readonly IAcmePdbParser acmePdbParser;
        readonly Globals globals;
        readonly IDispatcher dispatcher;
        readonly ISettingsManager settingsManager;
        readonly IServiceScope scope;
        readonly IViceBridge viceBridge;
        readonly IProjectPrgFileWatcher projectPdbFileWatcher;
        readonly CommandsManager commandsManager;
        public string AppName => "Modern VICE PDB Monitor";
        readonly Subscription closeOverlaySubscription;
        readonly Subscription prgFileChangedSubscription;
        readonly Subscription prgFilePathChangedSubscription;
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
        public RelayCommandAsync RunCommand { get; }
        public RelayCommand PauseCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommandAsync UpdatePdbCommand { get; }
        public Func<string?, CancellationToken, Task<string?>>? ShowCreateProjectFileDialogAsync { get; set; }
        public Func<string?, CancellationToken, Task<string?>>? ShowOpenProjectFileDialogAsync { get; set; }
        public Action? CloseApp { get; set; }
        public bool IsShowingSettings => OverlayContent is SettingsViewModel;
        public bool IsShowingProject => OverlayContent is ProjectViewModel;
        public bool IsShowingErrors { get; set; }
        public bool IsOpeningProject { get; private set; }
        public bool IsStartingDebugging { get; private set; }
        public bool IsParsingPdb { get; private set; }
        public bool IsBusy => IsOpeningProject || IsStartingDebugging || IsParsingPdb;
        public bool IsDebugging { get; private set; }
        public bool IsOverlayVisible => OverlayContent is not null;
        public bool IsViceConnected { get; private set; }
        public bool IsDebuggingPaused { get; private set; }
        public string RunCommandTitle => IsDebugging ? "Continue" : "Run";
        public string RunMenuCommandTitle => IsDebugging ? "_Continue" : "_Run";
        /// <summary>
        /// True when pdb file was changed.
        /// </summary>
        public bool IsUpdatedPdbAvailable { get; private set; }
        public ErrorMessagesViewModel ErrorMessagesViewModel { get; }
        public ScopedViewModel Content { get; private set; } = default!;
        public RegistersViewModel RegistersViewModel { get; private set; } = default!;
        public ScopedViewModel? OverlayContent { get; private set; }
        TaskCompletionSource stoppedExecution;
        Process? viceProcess;
        CancellationTokenSource? startDebuggingCts;
        readonly TaskFactory uiFactory;
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
            ISettingsManager settingsManager, ErrorMessagesViewModel errorMessagesViewModel, IServiceScope scope, IViceBridge viceBridge,
            IProjectPrgFileWatcher projectPdbFileWatcher, RegistersMapping registersMapping, RegistersViewModel registers)
        {
            this.logger = logger;
            this.acmePdbParser = acmePdbParser;
            this.globals = globals;
            this.dispatcher = dispatcher;
            this.settingsManager = settingsManager;
            this.scope = scope;
            this.viceBridge = viceBridge;
            this.projectPdbFileWatcher = projectPdbFileWatcher;
            RegistersViewModel = registers;
            uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
            commandsManager = new CommandsManager(this, uiFactory);
            closeOverlaySubscription = dispatcher.Subscribe<CloseOverlayMessage>(CloseOverlay);
            prgFileChangedSubscription = dispatcher.Subscribe<PrgFileChangedMessage>(PrgFileChanged);
            prgFilePathChangedSubscription = dispatcher.Subscribe<PrgFilePathChangedMessage>(PrgFilePathChanged);
            ErrorMessagesViewModel = errorMessagesViewModel;
            ShowSettingsCommand = commandsManager.CreateRelayCommand(ShowSettings, () => !IsShowingSettings);
            ShowProjectCommand = commandsManager.CreateRelayCommand(ShowProject, () => !IsShowingProject && IsProjectOpen);
            TestCommand = commandsManager.CreateRelayCommand(Test, () => !IsBusy);
            CreateProjectCommand = commandsManager.CreateRelayCommand(CreateProject, () => !IsBusy && !IsDebugging);
            OpenProjectFromPathCommand = commandsManager.CreateRelayCommand<string>(OpenProjectFromPath, _ => !IsBusy && !IsDebugging);
            OpenProjectCommand = commandsManager.CreateRelayCommand(OpenProject, () => !IsBusy && !IsDebugging);
            globals.PropertyChanged += Globals_PropertyChanged;
            CloseProjectCommand = commandsManager.CreateRelayCommand(CloseProject, () => IsProjectOpen && !IsDebugging);
            ExitCommand = new RelayCommand(() => CloseApp?.Invoke());
            ToggleErrorsVisibilityCommand = new RelayCommand(() => IsShowingErrors = !IsShowingErrors);
            RunCommand = commandsManager.CreateRelayCommandAsync(StartDebuggingAsync, () => IsProjectOpen && (!IsDebugging || IsDebuggingPaused));
            StopCommand = commandsManager.CreateRelayCommand(StopDebugging, () => IsDebugging);
            PauseCommand = commandsManager.CreateRelayCommand(PauseDebugging, () => IsDebugging && !IsDebuggingPaused && IsViceConnected);
            UpdatePdbCommand = commandsManager.CreateRelayCommandAsync(UpdatePdbAsync, () => !IsBusy && IsDebugging);
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
            stoppedExecution = new TaskCompletionSource();
            viceBridge.ConnectedChanged += ViceBridge_ConnectedChanged;
            viceBridge.ViceResponse += ViceBridge_ViceResponse;
            viceBridge.Start();
        }
        /// <summary>
        /// Unbound responses
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ViceBridge_ViceResponse(object? sender, ViceResponseEventArgs e)
        {
            Debug.WriteLine($"Got unbounded {e.Response.GetType().Name}");
            uiFactory.StartNew(() =>
            {
                switch (e.Response)
                {
                    case StoppedResponse:
                        IsDebuggingPaused = true;
                        stoppedExecution.SetResult();
                        break;
                    case ResumedResponse:
                        IsDebuggingPaused = false;
                        stoppedExecution = new TaskCompletionSource();
                        break;
                }
            });
        }

        void PrgFileChanged(object sender, PrgFileChangedMessage message)
        {
            _ = uiFactory.StartNew(() => IsUpdatedPdbAvailable = true);
        }
        void PrgFilePathChanged(object sender, PrgFilePathChangedMessage message)
        {
            _ = UpdatePdbAsync();
        }
        async Task UpdatePdbAsync()
        {
            IsParsingPdb = true;
            try
            {
                if (Project!.PrgPath is not null)
                {
#if DEBUG
                    await Task.Delay(5000);
#endif
                    var debugFiles = GetDebugFilesPath(Project.PrgPath);
                    globals.Pdb = await ParsePdbAsync(debugFiles);
                    IsUpdatedPdbAvailable = false;
                }
                else
                {
                    globals.Pdb = null;
                }
            }
            finally
            {
                IsParsingPdb = false;
            }
        }
        void ViceBridge_ConnectedChanged(object? sender, ConnectedChangedEventArgs e)
        {
            uiFactory.StartNew(() =>
            {
                IsViceConnected = e.IsConnected;
            });
        }
        internal void StopDebugging()
        {
            ClearAfterDebugging();
        }
        internal void PauseDebugging()
        {
            if (!IsDebuggingPaused)
            {
                viceBridge.EnqueueCommand(new PingCommand());
            }
        }
        internal async Task StartDebuggingAsync()
        {
            if (IsDebugging)
            {
                await ExitViceMonitorAsync();
            }
            else
            {
                const string title = "Start debugging";
                if (!IsViceConnected)
                {
                    if (viceProcess is null)
                    {
                        viceProcess = StartVice();
                        if (viceProcess is null)
                        {
                            dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, title, "Failed to start debugging"));
                            return;
                        }
                        viceProcess.Exited += ViceProcess_Exited;
                    }
                }
                IsStartingDebugging = true;
                IsDebugging = true;
                try
                {
                    if (IsUpdatedPdbAvailable)
                    {
                        await UpdatePdbAsync();
                    }
                    startDebuggingCts = new CancellationTokenSource();
                    if (!viceBridge.IsConnected)
                    {
                        await viceBridge.WaitForConnectionStatusChangeAsync(startDebuggingCts.Token);
                    }
                    // make sure vice isn't in paused state
                    if (IsDebuggingPaused)
                    {
                        await ExitViceMonitorAsync();
                    }
                    await RegistersViewModel.InitAsync();
                    var command = viceBridge.EnqueueCommand(new AutoStartCommand(runAfterLoading: false, 0, globals.FullPrgPath!));
                    await command.Response;
                    await stoppedExecution.Task;
                    await Task.Delay(1000);
                    //await ExitViceMonitorAsync();
                    await RegistersViewModel.SetStartAddressAsync(default);
                }
                catch (OperationCanceledException)
                {
                    ClearAfterDebugging();
                }
                finally
                {
                    IsStartingDebugging = false;
                }
            }
        }
        internal async Task ResetViceAsync(CancellationToken ct)
        {
            var command = viceBridge.EnqueueCommand(new ResetCommand(ResetMode.Soft));
            await command.Response;
        }
        internal async Task ExitViceMonitorAsync()
        {
            await EnqueueCommandAndWaitForResponseAsync(new ExitCommand());
        }

        internal async Task<TResponse?> EnqueueCommandAndWaitForResponseAsync<TResponse>(ViceCommand<TResponse> command, TimeSpan? timeout = default, CancellationToken ct = default)
            where TResponse: ViceResponse
        {
            const string title = "Execute VICE command";
            viceBridge.EnqueueCommand(command);
            try
            {
                return await command.Response.AwaitWithTimeoutAsync(timeout ?? TimeSpan.FromSeconds(5), ct);
            }
            catch(TimeoutException)
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Warning, title, $"Timeout while executing {command.GetType().Name}"));
                logger.Log(LogLevel.Warning, "Timeout while executing {Command}", command.GetType().Name);
                return null;
            }
            catch (Exception ex)
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Warning, title, $"General failure while executing {command.GetType().Name}"));
                logger.Log(LogLevel.Warning, ex, "General failure while executing {Command}", command.GetType().Name);
                return null;
            }
        }
        void ViceProcess_Exited(object? sender, EventArgs e)
        {
            viceProcess!.Exited -= ViceProcess_Exited;
            viceProcess = null;
            StopDebugging();
        }

        internal void ClearAfterDebugging()
        {
            logger.LogDebug("Cleaning after debugging");
            //if (viceProcess is not null)
            //{
            //    if (viceBridge.IsConnected)
            //    {
            //        logger.LogDebug("VICE process running and bridge is connected, will try Quit command");
            //        var quitCommand = new ViceBridgeCommand.QuitCommand();
            //        viceBridge.EnqueueCommand(quitCommand);
            //        (bool success, var result) = await quitCommand.Response.AwaitWithTimeout(TimeSpan.FromSeconds(10));
            //        bool isQuitSuccess = true;
            //        if (!success)
            //        {
            //            logger.LogDebug("Timeout while waiting for VICE response to Quit command");
            //            isQuitSuccess = false;
            //        }
            //        else
            //        {
            //            if (result!.ErrorCode != ViceBridgeCommand.ErrorCode.OK)
            //            {
            //                logger.LogDebug("VICE returned {ErrorCode} to Quit command", result!.ErrorCode);
            //                isQuitSuccess = false;
            //            }
            //        }
            //        if (!isQuitSuccess)
            //        {
            //            viceProcess.Kill();
            //        }
            //        bool waitForKillSuccess = viceProcess.WaitForExit(5000);
            //        if (!waitForKillSuccess)
            //        {
            //            logger.LogWarning("Couldn't kill VICE process");
            //        }
            //        viceProcess.Dispose();
            //    }
            //}
            IsDebugging = false;
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
            where T : ScopedViewModel
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
                    OnPropertyChanged(nameof(IsProjectOpen));
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
                    //string arguments = $"-binarymonitor -autostartprgmode 1 {globals.FullPrgPath}";
                    string arguments = $"-binarymonitor";
                    return Process.Start(path, arguments);
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
            IsOpeningProject = true;
            try
            {
                if (!File.Exists(path))
                {
                    dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, ErrorTitle, $"Project file {path} does not exist."));
                    return false;
                    //globals.Pdb = await ParsePdbAsync(GetPdbPath(prgPath));
                }
                globals.Pdb = null;
                var project = settingsManager.Load<Project>(path)!;
                globals.Project = project;
                if (project!.PrgPath is not null)
                {
                    var debugFiles = GetDebugFilesPath(project.PrgPath);
                    globals.Pdb = await ParsePdbAsync(debugFiles);
                }
                globals.Settings.AddRecentProject(path);
            }
            catch (Exception ex)
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, ErrorTitle, ex.Message));
            }
            finally
            {
                IsOpeningProject = false;
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
        internal DebugFiles GetDebugFilesPath(string prgPath)
        {
            string directory = Path.Combine(globals.ProjectDirectory!, Path.GetDirectoryName(prgPath) ?? "");
            return new DebugFiles(
                Path.Combine(directory, globals.GetReportFileName(prgPath)),
                Path.Combine(directory, globals.GetLabelsFileName(prgPath)));
        }
        internal async Task<AcmePdb?> ParsePdbAsync(DebugFiles debugFiles)
        {
            if (!File.Exists(debugFiles.Report))
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Failed parsing report", $"Report file {debugFiles.Report} does not exist"));
                return null;
            }
            if (!File.Exists(debugFiles.Labels))
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Failed parsing labels", $"Labels file {debugFiles.Labels} does not exist"));
                return null;
            }
            IsParsingPdb = true;
            try
            {
                var result = await Task.Run(() => acmePdbParser.ParseAsync(debugFiles));
                if (result.Errors.Length > 0)
                {
                    string errorMessage = string.Join('\n', result.Errors.Select(e => e.ErrorText));
                    dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Failed parsing PDB", errorMessage));
                    return null;
                }
                else
                {
                    return result.ParsedData;
                }
            }
            catch (Exception ex)
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Failed parsing PDB", ex.Message));
                return null;
            }
            finally
            {
                IsParsingPdb = false;
            }
        }
        protected override void OnPropertyChanged([CallerMemberName] string name = default!)
        {
            base.OnPropertyChanged(name);
            switch (name)
            {
                case nameof(Project):
                    if (Project?.PrgPath is not null)
                    {
                        projectPdbFileWatcher.Start(globals.ProjectDirectory!, Project.PrgPath);
                    }
                    else
                    {
                        projectPdbFileWatcher.Stop();
                    }
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                viceBridge.ConnectedChanged -= ViceBridge_ConnectedChanged;
                viceBridge.ViceResponse -= ViceBridge_ViceResponse;
                closeOverlaySubscription.Dispose();
                prgFileChangedSubscription.Dispose();
                prgFilePathChangedSubscription.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
