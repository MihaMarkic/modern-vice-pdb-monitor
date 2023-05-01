using Modern.Vice.PdbMonitor.Core;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class ExecutionStatusViewModel: NotifiableObject
{
    public bool IsStartingDebugging { get; internal set; }
    public bool IsDebugging { get; internal set; }
    public bool IsDebuggingPaused { get; internal set; }
    public bool IsSteppingInto { get; internal set; }
    public bool IsSteppingOver { get; internal set; }
    public bool IsStepping => IsSteppingInto || IsSteppingOver;
}
