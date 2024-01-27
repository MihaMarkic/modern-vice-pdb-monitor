namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public interface IStatusInfoViewModel
{
    bool EffectiveVisibility { get; }
    ushort? ExecutionAddress { get; }
    bool ExecutionAddressVisible { get; }
    DebuggerStepMode StepMode { get;  }
}
