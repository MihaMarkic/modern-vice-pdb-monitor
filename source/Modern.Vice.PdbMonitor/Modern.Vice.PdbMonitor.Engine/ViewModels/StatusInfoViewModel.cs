using System.ComponentModel;
using Modern.Vice.PdbMonitor.Core;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public class StatusInfoViewModel : NotifiableObject, IStatusInfoViewModel
{
    readonly RegistersViewModel registersViewModel;
    readonly ExecutionStatusViewModel executionStatusViewModel;
    readonly ProfilerViewModel profilerViewModel;
    public ushort? ExecutionAddress { get; set; }
    public bool ExecutionAddressVisible { get; set; }
    public bool EffectiveVisibility { get; private set; }
    public DebuggerStepMode StepMode { get; set; }
    public StatusInfoViewModel(RegistersViewModel registersViewModel, ExecutionStatusViewModel executionStatusViewModel,
        ProfilerViewModel profilerViewModel)
    {
        this.registersViewModel = registersViewModel;
        this.executionStatusViewModel = executionStatusViewModel;
        this.profilerViewModel = profilerViewModel;
        registersViewModel.PropertyChanged += RegistersViewModel_PropertyChanged;
        executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
        profilerViewModel.PropertyChanged += ProfilerViewModel_PropertyChanged;
    }

    void UpdateStatusText()
    {
        OnPropertyChanged(nameof(StatusText));
    }
    public string StatusText
    {
        get
        {
            if (profilerViewModel.IsStarting)
            {
                return "Starting profiler";
            }
            else if (profilerViewModel.IsStopping)
            {
                return "Stopping profiler";
            }
            else if (profilerViewModel.IsActive)
            {
                return "Profiling";
            }
            else if (executionStatusViewModel.IsDebuggingPaused)
            {
                return "Debugging paused";
            }
            else if (executionStatusViewModel.IsDebugging)
            {
                return "Debugging";
            }
            else
            {
                return string.Empty;
            }
        }
    }

    private void ProfilerViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(profilerViewModel.IsActive):
            case nameof(profilerViewModel.IsStopping):
            case nameof(profilerViewModel.IsStarting):
                UpdateStatusText();
                break;
        }
    }

    private void ExecutionStatusViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(executionStatusViewModel.IsDebuggingPaused):
                if (executionStatusViewModel.IsDebuggingPaused)
                {
#if DEBUG
                    EffectiveVisibility = true;
#else
                    _ = DelayEffectiveVisibility();
#endif
                }
                else
                {
                    visibilityCts?.Cancel();
                    EffectiveVisibility = false;
                }
                break;
        }
    }

    CancellationTokenSource? visibilityCts;
    async Task DelayEffectiveVisibility()
    {
        visibilityCts?.Cancel();
        visibilityCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(200, visibilityCts.Token);
            EffectiveVisibility = true;
        }
        catch (OperationCanceledException) { }
    }

    private void RegistersViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(RegistersViewModel.Current):
                ExecutionAddress = registersViewModel.Current.PC;
                break;
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            registersViewModel.PropertyChanged -= RegistersViewModel_PropertyChanged;
            executionStatusViewModel.PropertyChanged -= ExecutionStatusViewModel_PropertyChanged;
            profilerViewModel.PropertyChanged -= ProfilerViewModel_PropertyChanged;
        }
        base.Dispose(disposing);
    }
}

public class DesignStatusInfoViewModel : IStatusInfoViewModel
{
    public bool EffectiveVisibility => true;

    public ushort? ExecutionAddress => 0x8aae;

    public bool ExecutionAddressVisible => true;

    public DebuggerStepMode StepMode => DebuggerStepMode.High;

    public string StatusText => "Debugging paused";
}
