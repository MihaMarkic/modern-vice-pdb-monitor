using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Core.Common.Compiler;
using Modern.Vice.PdbMonitor.Engine.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class MainViewModel : NotifiableObject
{
    readonly ILogger<MainViewModel> logger;
    readonly IDispatcher dispatcher;
    readonly ISettingsManager settingsManager;
    readonly IServiceScope scope;
    readonly IViceBridge viceBridge;
    readonly IProjectPrgFileWatcher projectPdbFileWatcher;
    readonly IServiceProvider serviceProvider;
    readonly CommandsManager commandsManager;
    readonly ExecutionStatusViewModel executionStatusViewModel;
    public Globals Globals { get; }
    readonly ISubscription closeOverlaySubscription;
    readonly ISubscription prgFileChangedSubscription;
    readonly ISubscription prgFilePathChangedSubscription;
    readonly ISubscription showModalDialogMessageSubscription;
    public bool IsProjectOpen => Globals.Project is not null;
    public ObservableCollection<string> RecentProjects => Globals.Settings.RecentProjects;
    public RelayCommand ShowSettingsCommand { get; }
    public RelayCommand ShowProjectCommand { get; }
    public RelayCommand TestCommand { get; }
    public RelayCommand<CompilerType> CreateProjectCommand { get; }
    public RelayCommand<string> OpenProjectFromPathCommand { get; }
    public RelayCommand OpenProjectCommand { get; }
    public RelayCommand CloseProjectCommand { get; }
    public RelayCommand ExitCommand { get; }
    public RelayCommand ToggleErrorsVisibilityCommand { get; }
    public RelayCommandAsync RunCommand { get; }
    public RelayCommand PauseCommand { get; }
    public RelayCommandAsync StopCommand { get; }
    public RelayCommandAsync StepIntoCommand { get; }
    public RelayCommandAsync StepOverCommand { get; }
    public RelayCommandAsync UpdatePdbCommand { get; }
    public RelayCommand ToggleIsAutoUpdateEnabledCommand { get; }
    public RelayCommand ShowMessagesHistoryCommand { get; }
    /// <summary>
    /// When true, <see cref="UpdatePdbCommand"/> is called automatically upon detected changes.
    /// </summary>
    public bool IsAutoUpdateEnabled { get; set; }
    public Func<OpenFileDialogModel, CancellationToken, Task<string?>>? ShowCreateProjectFileDialogAsync { get; set; }
    public Func<OpenFileDialogModel, CancellationToken, Task<string?>>? ShowOpenProjectFileDialogAsync { get; set; }
    public Action<ShowModalDialogMessageCore>? ShowModalDialog { get; set; }
    public Action? CloseApp { get; set; }
    public Action? ShowMessagesHistoryContent { get; set; }
    public bool IsShowingSettings => OverlayContent is SettingsViewModel;
    public bool IsShowingProject => OverlayContent is ProjectViewModel;
    public bool IsShowingErrors { get; set; }
    public bool IsParsingPdb { get; private set; }
    public bool IsBusy => executionStatusViewModel.IsOpeningProject || executionStatusViewModel.IsStartingDebugging || IsParsingPdb;
    public bool IsStartingDebugging => executionStatusViewModel.IsDebugging;
    public bool IsDebugging => executionStatusViewModel.IsDebugging;
    public bool IsDebuggingPaused => executionStatusViewModel.IsDebuggingPaused;
    public bool IsDebuggerStepping => executionStatusViewModel.IsStepping;
    public bool IsOverlayVisible => OverlayContent is not null;
    public bool IsViceConnected { get; private set; }
    public string RunCommandTitle => executionStatusViewModel.IsDebugging ? "Continue" : "Run";
    public string RunMenuCommandTitle => executionStatusViewModel.IsDebugging ? "_Continue" : "_Run";
    /// <summary>
    /// True when pdb file was changed.
    /// </summary>
    public bool IsUpdatedPdbAvailable { get; private set; }
    public ErrorMessagesViewModel ErrorMessagesViewModel { get; }
    //public ScopedViewModel Content { get; private set; }
    public RegistersViewModel RegistersViewModel { get; }
    public BreakpointsViewModel BreakpointsViewModel { get; }
    public VariablesViewModel VariablesViewModel { get; }
    public DebuggerViewModel DebuggerViewModel { get; }
    public TraceOutputViewModel TraceOutputViewModel { get; }
    public MessagesHistoryViewModel MessagesHistoryViewModel { get; }
    public ScopedViewModel? OverlayContent { get; private set; }
    TaskCompletionSource stoppedExecution;
    TaskCompletionSource resumedExecution;
    Process? viceProcess;
    CancellationTokenSource? startDebuggingCts;
    readonly TaskFactory uiFactory;
    public MainViewModel(ILogger<MainViewModel> logger, Globals globals, IDispatcher dispatcher,
        ISettingsManager settingsManager, ErrorMessagesViewModel errorMessagesViewModel, IServiceScope scope, IViceBridge viceBridge,
        IProjectPrgFileWatcher projectPdbFileWatcher, IServiceProvider serviceProvider, RegistersMapping registersMapping, RegistersViewModel registers, 
        ExecutionStatusViewModel executionStatusViewModel, BreakpointsViewModel breakpointsViewModel,
        VariablesViewModel variablesViewModel, DebuggerViewModel debuggerViewModel, 
        TraceOutputViewModel traceOutputViewModel, MessagesHistoryViewModel messagesHistoryViewModel)
    {
        this.logger = logger;
        this.Globals = globals;
        this.dispatcher = dispatcher;
        this.settingsManager = settingsManager;
        this.scope = scope;
        this.viceBridge = viceBridge;
        this.projectPdbFileWatcher = projectPdbFileWatcher;
        this.executionStatusViewModel = executionStatusViewModel;
        this.serviceProvider = serviceProvider;
        RegistersViewModel = registers;
        BreakpointsViewModel = breakpointsViewModel;
        VariablesViewModel = variablesViewModel;
        DebuggerViewModel = debuggerViewModel;
        TraceOutputViewModel = traceOutputViewModel;
        MessagesHistoryViewModel = messagesHistoryViewModel;
        executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
        uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        commandsManager = new CommandsManager(this, uiFactory);
        closeOverlaySubscription = dispatcher.Subscribe<CloseOverlayMessage>(CloseOverlay);
        prgFileChangedSubscription = dispatcher.Subscribe<PrgFileChangedMessage>(PrgFileChanged);
        prgFilePathChangedSubscription = dispatcher.Subscribe<PrgFilePathChangedMessage>(PrgFilePathChanged);
        showModalDialogMessageSubscription = dispatcher.Subscribe<ShowModalDialogMessageCore>(OnShowModalDialog);
        ErrorMessagesViewModel = errorMessagesViewModel;
        ShowSettingsCommand = commandsManager.CreateRelayCommand(ShowSettings, () => !IsShowingSettings);
        ShowProjectCommand = commandsManager.CreateRelayCommand(ShowProject, () => !IsShowingProject && IsProjectOpen);
        TestCommand = commandsManager.CreateRelayCommand(Test, () => !IsBusy);
        CreateProjectCommand = commandsManager.CreateRelayCommand<CompilerType>(CreateProject, _ => !IsBusy && !IsDebugging);
        OpenProjectFromPathCommand = commandsManager.CreateRelayCommand<string>(OpenProjectFromPath, _ => !IsBusy && !IsDebugging);
        OpenProjectCommand = commandsManager.CreateRelayCommand(OpenProject, () => !IsBusy && !IsDebugging);
        globals.PropertyChanged += Globals_PropertyChanged;
        CloseProjectCommand = commandsManager.CreateRelayCommand(CloseProject, () => IsProjectOpen && !IsDebugging);
        ExitCommand = new RelayCommand(() => CloseApp?.Invoke());
        ToggleErrorsVisibilityCommand = new RelayCommand(() => IsShowingErrors = !IsShowingErrors);
        RunCommand = commandsManager.CreateRelayCommandAsync(
            StartDebuggingAsync, () => IsProjectOpen && (!IsDebugging || IsDebuggingPaused && !IsDebuggerStepping));
        StopCommand = commandsManager.CreateRelayCommandAsync(StopDebuggingAsync, () => IsDebugging);
        PauseCommand = commandsManager.CreateRelayCommand(
            PauseDebugging, () => IsDebugging && !IsDebuggingPaused && IsViceConnected && !IsDebuggerStepping);
        StepIntoCommand = commandsManager.CreateRelayCommandAsync(
            StepIntoAsync, () => IsDebugging && IsDebuggingPaused && !IsDebuggerStepping);
        StepOverCommand = commandsManager.CreateRelayCommandAsync(
            StepOverAsync, () => IsDebugging && IsDebuggingPaused && !IsDebuggerStepping);
        UpdatePdbCommand = commandsManager.CreateRelayCommandAsync(
            UpdatePdbAsync, () => !IsBusy && !IsDebugging && IsUpdatedPdbAvailable);
        ToggleIsAutoUpdateEnabledCommand = commandsManager.CreateRelayCommand(ToggleIsAutoUpdateEnabled, () => true);
        // by default opens most recent project
        if (globals.Settings.RecentProjects.Count > 0)
        {
            OpenProjectFromPath(globals.Settings.RecentProjects[0]);
        }
        ShowMessagesHistoryCommand = commandsManager.CreateRelayCommand(ShowMessagesHistory, () => MessagesHistoryViewModel.IsAvailable);
        IsAutoUpdateEnabled = globals.Settings.IsAutoUpdateEnabled;
        stoppedExecution = new TaskCompletionSource();
        resumedExecution = new TaskCompletionSource();
        viceBridge.ConnectedChanged += ViceBridge_ConnectedChanged;
        viceBridge.ViceResponse += ViceBridge_ViceResponse;
        viceBridge.Start();
        if (!Directory.Exists(globals.Settings.VicePath))
        {
            SwitchOverlayContent<SettingsViewModel>();
        }
    }
    /// <summary>
    /// Relays execution status
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void ExecutionStatusViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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

    bool traceCharAvailable;
    /// <summary>
    /// Unbound responses
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void ViceBridge_ViceResponse(object? sender, ViceResponseEventArgs e)
    {
        //Debug.WriteLine($"Got unbounded {e.Response.GetType().Name}");

        switch (e.Response)
        {
            case StoppedResponse:
                executionStatusViewModel.IsViceStopped = true;
                uiFactory.StartNew(() =>
                {
                    if (executionStatusViewModel.IsDebugging)
                    {
                        if (traceCharAvailable)
                        {
                            traceCharAvailable = false;
                            _ = TraceOutputViewModel.LoadTraceLineAsync(CancellationToken.None);                           
                        }
                        else
                        {
                            executionStatusViewModel.IsDebuggingPaused = true;
                            stoppedExecution.SetResult();
                            stoppedExecution = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                        }
                    }
                    else
                    {
                        // when not debugging, stopping VICE is not desired. i.e. when a checkpoint is added
                        //viceBridge.EnqueueCommand(new ExitCommand());
                    }
                });
                break;
            case ResumedResponse:
                executionStatusViewModel.IsViceStopped = false;
                uiFactory.StartNew(() =>
                {
                    if (executionStatusViewModel.IsDebugging)
                    {
                        // when processing is disabled, enable it each time it hits Resume
                        if (executionStatusViewModel.IsProcessingDisabled)
                        {
                            executionStatusViewModel.IsProcessingDisabled = false;
                        }
                        else
                        {
                            executionStatusViewModel.IsDebuggingPaused = false;
                            resumedExecution.SetResult();
                            resumedExecution = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                        }
                    }
                });
                break;
            case CheckpointInfoResponse checkpointInfoResponse:
                Debug.WriteLine($"CHECKPOINT: {checkpointInfoResponse.StartAddress:X4}");
                uiFactory.StartNew(() =>
                {
                    if (checkpointInfoResponse.CheckpointNumber == TraceOutputViewModel.CheckpointNumber)
                    {
                        executionStatusViewModel.IsProcessingDisabled = true;
                        traceCharAvailable = true;
                    }
                });
                break;
        }

    }

    void PrgFileChanged(PrgFileChangedMessage message)
    {
        _ = uiFactory.StartNew(async () =>
        {
            IsUpdatedPdbAvailable = true;
            if (IsAutoUpdateEnabled)
            {
                await UpdatePdbAsync();
            }
        });
    }
    void PrgFilePathChanged(PrgFilePathChangedMessage message)
    {
        _ = UpdatePdbAsync();
    }
    void OnShowModalDialog(ShowModalDialogMessageCore message)
    {
        ShowModalDialog?.Invoke(message);
    }
    async Task UpdatePdbAsync()
    {
        IsParsingPdb = true;
        try
        {
            if (Globals.Project!.PrgPath is not null)
            {
                IsParsingPdb = true;
                try
                {
                    bool newDebuggingIsApplied = await ParseDebugSymbolsAsync(Globals.Project);
                    if (newDebuggingIsApplied)
                    {
                        await dispatcher.DispatchAsync(new DebugDataChangedMessage());
                    }
                }
                finally
                {
                    IsParsingPdb = false;
                }
                IsUpdatedPdbAvailable = false;
            }
            else
            {
                Globals.Project.DebugSymbols = null;
                await dispatcher.DispatchAsync(new DebugDataChangedMessage());
            }
        }
        finally
        {
            IsParsingPdb = false;
        }
    }
    /// <summary>
    /// Creates, uses and disposes correct parser based on project's CompilerType.
    /// </summary>
    /// <param name="project"></param>
    /// <returns>True when new debugging data is applied, false otherwise</returns>
    async Task<bool> ParseDebugSymbolsAsync(Project project)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var projectFactory = scope.ServiceProvider.GetRequiredService<IProjectFactory>();
            var parser = projectFactory.GetParser(project.CompilerType);
            (var pdb, var errorMessage) = await parser.ParseDebugSymbolsAsync(project.Directory!, project.PrgPath!, CancellationToken.None);
            if (pdb is not null)
            {
                project.DebugSymbols = pdb;
                return true;
            }
            else
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Failed parsing", errorMessage ?? ""));
            }
            return false;
        }
    }
    void ViceBridge_ConnectedChanged(object? sender, ConnectedChangedEventArgs e)
    {
        uiFactory.StartNew(() =>
        {
            IsViceConnected = e.IsConnected;
        });
    }
    internal void ToggleIsAutoUpdateEnabled()
    {
        IsAutoUpdateEnabled = !IsAutoUpdateEnabled;
    }
    internal async Task StopDebuggingAsync()
    {
        await BreakpointsViewModel.DisarmAllBreakpoints(CancellationToken.None);
        await ClearAfterDebuggingAsync();
    }
    internal void PauseDebugging()
    {
        if (!IsDebuggingPaused)
        {
            viceBridge.EnqueueCommand(new PingCommand(), resumeOnStopped: false);
        }
    }
    internal async Task StartDebuggingAsync()
    {
        if (IsDebugging)
        {
            await DebuggerViewModel.ContinueAsync();
        }
        else
        {
            MessagesHistoryViewModel.Clear();
            MessagesHistoryViewModel.Start();
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
            executionStatusViewModel.InitializeForDebugging();
            executionStatusViewModel.IsStartingDebugging = true;
            executionStatusViewModel.IsDebugging = true;
            try
            {
                bool hasPdbChanged = IsUpdatedPdbAvailable;
                if (IsUpdatedPdbAvailable)
                {
                    await UpdatePdbAsync();
                }
                startDebuggingCts = new CancellationTokenSource();
                if (!viceBridge.IsConnected)
                {
                    await viceBridge.WaitForConnectionStatusChangeAsync(startDebuggingCts.Token);
                }
                await BreakpointsViewModel.RearmBreakpoints(hasPdbChanged, startDebuggingCts.Token);
                await TraceOutputViewModel.CreateTraceCheckpointAsync(startDebuggingCts.Token);
                // make sure vice isn't in paused state
                if (IsDebuggingPaused)
                {
                    await DebuggerViewModel.ExitViceMonitorAsync();
                }
                await RegistersViewModel.InitAsync();
                executionStatusViewModel.IsStartingDebugging = false;

                var command = viceBridge.EnqueueCommand(
                    new AutoStartCommand(runAfterLoading: Globals.Project!.AutoStartMode == DebugAutoStartMode.Vice, 
                        0, Globals.Project.FullPrgPath!),
                    resumeOnStopped: false);
                await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command);
                await stoppedExecution.Task;

                if (Globals.Project!.StopAtLabel != "[None]")
                {
                    if (!string.IsNullOrWhiteSpace(Globals.Project!.StopAtLabel) && Globals.Project.DebugSymbols is not null
                        && Globals.Project.DebugSymbols.Labels.TryGetValue(Globals.Project!.StopAtLabel, out var label))
                    {
                        var checkpoint = viceBridge.EnqueueCommand(
                            new CheckpointSetCommand(label.Address, label.Address, true, true, CpuOperation.Exec, true),
                            resumeOnStopped: true);
                        await checkpoint.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, checkpoint);
                    }
                    else
                    {
                        dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Warning, "Start debugging",
                            $"Couldn't set auto start checkpoint for label {Globals.Project!.StopAtLabel}"));
                    }
                }

                if (Globals.Project!.AutoStartMode == DebugAutoStartMode.AtAddress)
                {
                    if (Globals.Project.DebugSymbols is not null && Globals.Project.DebugSymbols.Labels.TryGetValue("start", out var label))
                    {
                        await RegistersViewModel.SetStartAddressAsync(label!.Address, startDebuggingCts.Token);
                    }
                    else
                    {
                        dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Warning, "Start debugging", 
                            $"Couldn't auto start using AtAddress for label {Globals.Project!.StopAtLabel}"));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                await ClearAfterDebuggingAsync();
            }
            finally
            {
                executionStatusViewModel.IsStartingDebugging = false;
            }
        }
    }
    internal async Task ResetViceAsync(CancellationToken ct)
    {
        var command = viceBridge.EnqueueCommand(new ResetCommand(ResetMode.Soft), resumeOnStopped: false);
        await command.Response;
    }
    
    void ViceProcess_Exited(object? sender, EventArgs e)
    {
        viceProcess!.Exited -= ViceProcess_Exited;
        viceProcess = null;
        _ = StopDebuggingAsync();
    }

    internal async Task ClearAfterDebuggingAsync()
    {
        logger.LogDebug("Cleaning after debugging");
        executionStatusViewModel.IsDebugging = false;
        await TraceOutputViewModel.ClearTraceCheckpointAsync();
        if (viceBridge?.IsConnected ?? false)
        {
            if (Globals.Settings.ResetOnStop)
            {
                await ResetViceAsync(CancellationToken.None);
            }
            else
            {
                var command = viceBridge.EnqueueCommand(new ExitCommand(),  resumeOnStopped: true);
                await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command);
            }
        }
    }

    /// <summary>
    /// Kills VICE process if any associated.
    /// </summary>
    /// <returns></returns>
    /// <remarks>Not tested.</remarks>
    internal async Task KillViceAsync()
    {
        if (viceProcess is not null && viceBridge?.IsConnected == true)
        {
            logger.LogDebug("VICE process running and bridge is connected, will try Quit command");
            var quitCommand = viceBridge.EnqueueCommand(new QuitCommand(), resumeOnStopped: false);
            var result = await quitCommand.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, quitCommand, TimeSpan.FromSeconds(10));
            if (result is not null)
            {
                logger.LogDebug("Timeout while waiting for VICE response to Quit command");
                viceProcess.Kill();
                bool waitForKillSuccess = viceProcess.WaitForExit(5000);
                if (!waitForKillSuccess)
                {
                    logger.LogWarning("Couldn't kill VICE process");
                }
                viceProcess.Dispose();
            }
        }
    }

    internal void CloseOverlay(CloseOverlayMessage message)
    {
        if (OverlayContent is not null)
        {
            OverlayContent.Dispose();
            OverlayContent = null;
        }
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
    internal async Task StepIntoAsync()
    {
        await DebuggerViewModel.StepIntoAsync();
    }
    internal async Task StepOverAsync()
    {
        await DebuggerViewModel.StepOverAsync();
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
                OnPropertyChanged(nameof(IsProjectOpen));
                break;
            case Globals.ProjectDirectory:
                UpdateDirectoryChangesTracker();
                break;
        }
    }
    void UpdateDirectoryChangesTracker()
    {
        if (Globals.Project?.PrgPath is not null)
        {
            string fullPrgPath = Path.Combine(Globals.Project!.Directory!, Globals.Project.PrgPath);
            string prgDirectoryPath = Path.GetDirectoryName(fullPrgPath)!;
            string filter = Path.GetFileName(fullPrgPath);
            projectPdbFileWatcher.Start(prgDirectoryPath, filter);
        }
        else
        {
            projectPdbFileWatcher.Stop();
        }
    }
    internal void CloseProject()
    {
        DebuggerViewModel.CloseProject();
        Globals.Project = null;
    }
    internal Process? StartVice()
    {
        if (!string.IsNullOrWhiteSpace(Globals.Settings.VicePath))
        {
            string path = Path.Combine(Globals.Settings.VicePath, "bin", "x64sc.exe");
            try
            {
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

    void ShowMessagesHistory() => ShowMessagesHistoryContent?.Invoke();
    void Test()
    {
        if (!string.IsNullOrWhiteSpace(Globals.Settings.VicePath))
        {
            string path = Path.Combine(Globals.Settings.VicePath, "bin", "x64sc.exe");
            var proc = Process.Start(path, "-binarymonitor");
        }
    }
    internal async void OpenProjectFromPath(string? path)
    {
        // runs async because it manipulates most recent list
        await Task.Delay(1);
        CloseProject();
        await OpenProjectFromPathInternalAsync(path);
    }
    internal async Task<bool> OpenProjectFromPathInternalAsync(string? path, CancellationToken ct = default)
    {
        const string ErrorTitle = "Failed opening project";
        if (path is null)
        {
            return false;
        }
        executionStatusViewModel.IsOpeningProject = true;
        try
        {
            if (!File.Exists(path))
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, ErrorTitle, $"Project file {path} does not exist."));
                return false;
            }
            var projectConfiguration = settingsManager.Load<ProjectConfiguration>(path)!;
            if (projectConfiguration is null)
            {
                return false;
            }
            var sourceLanguage = GetCompilerLanguage(projectConfiguration.CompilerType);
            var project = Project.FromConfiguration(projectConfiguration, sourceLanguage);
            project.File = path;
            Globals.Project = project;
            if (project!.PrgPath is not null)
            {
                await ParseDebugSymbolsAsync(project);
                await LoadBreakpointsAsync(ct);
                UpdateDirectoryChangesTracker();
            }
            Globals.Settings.AddRecentProject(path);
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, ErrorTitle, ex.Message));
        }
        finally
        {
            executionStatusViewModel.IsOpeningProject = false;
        }
        return false;
    }
    internal async Task LoadBreakpointsAsync(CancellationToken ct = default)
    {
        var settings = Globals.LoadBreakpoints();
        await BreakpointsViewModel.LoadBreakpointsFromSettingsAsync(settings);
    }
    public async void OpenProject()
    {
        if (ShowOpenProjectFileDialogAsync is not null)
        {
            var model = new OpenFileDialogModel(
                Globals.Settings.LastAccessedDirectory,
                "Modern PDB Debugger files",
                "*.mapd");
            string? projectPath = await ShowOpenProjectFileDialogAsync(model, CancellationToken.None);
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                _ = OpenProjectFromPathInternalAsync(projectPath);
            }
        }
    }
    public async void CreateProject(CompilerType compilerType)
    {
        if (ShowCreateProjectFileDialogAsync is not null)
        {
            var model = new OpenFileDialogModel(
                                        Globals.Settings.LastAccessedDirectory,
                                        "Modern PDB Debugger files",
                                        "*.mapd");
            string? projectPath = await ShowCreateProjectFileDialogAsync(model, CancellationToken.None);
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                bool success = CreateProject(compilerType, projectPath);
                if (success)
                {
                    Globals.Settings.AddRecentProject(projectPath);
                }
            }
        }
    }
    internal SourceLanguage GetCompilerLanguage(CompilerType compilerType)
    {
        // custom scope is used for compiler creation to figure out language
        using (var scope = serviceProvider.CreateScope())
        {
            var projectFactory = scope.ServiceProvider.GetRequiredService<IProjectFactory>();
            var compiler = projectFactory.GetCompiler(compilerType);
            return compiler.Language;
        }
    }
    internal bool CreateProject(CompilerType compilerType, string projectPath)
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
                var sourceLanguage = GetCompilerLanguage(compilerType);
                var project = Project.Create(projectPath, compilerType, sourceLanguage);
                settingsManager.Save(project.ToConfiguration(), projectPath, false);
                Globals.Project = project;
                ShowProject();
                return true;
            }
            catch (Exception ex)
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Failed creating project", ex.Message));
            }
        }
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Globals.Settings.IsAutoUpdateEnabled = IsAutoUpdateEnabled;
            viceBridge.ConnectedChanged -= ViceBridge_ConnectedChanged;
            viceBridge.ViceResponse -= ViceBridge_ViceResponse;
            executionStatusViewModel.PropertyChanged -= ExecutionStatusViewModel_PropertyChanged;
            Globals.PropertyChanged -= Globals_PropertyChanged;
            closeOverlaySubscription.Dispose();
            prgFileChangedSubscription.Dispose();
            prgFilePathChangedSubscription.Dispose();
            showModalDialogMessageSubscription.Dispose();
        }
        base.Dispose(disposing);
    }
}
