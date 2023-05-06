using Modern.Vice.PdbMonitor.Core;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class ExecutionStatusViewModel : NotifiableObject
{
    readonly object sync = new object();
    bool isProcessingDisabled;
    /// <summary>
    /// Signals that processing of events shouldn't be invoked
    /// because state is waiting for a special event. So far such event is only tracing.
    /// </summary>
    /// <threadsafety>Thread safe.</threadsafety>
    /// <remarks>
    /// If bounding, take care of threading, since the value can change outside UI thread.
    /// </remarks>
    public bool IsProcessingDisabled
    {
        get
        {
            lock (sync)
            {
                return isProcessingDisabled;
            }
        }

        internal set
        {
            lock (sync)
            {
                if (isProcessingDisabled != value)
                {
                    isProcessingDisabled = value;
                    OnPropertyChanged(nameof(IsProcessingDisabled));
                }
            }
        }
    }
    public bool IsStartingDebugging { get; internal set; }
    public bool IsDebugging { get; internal set; }
    public bool IsDebuggingPaused { get; internal set; }
    public bool IsSteppingInto { get; internal set; }
    public bool IsSteppingOver { get; internal set; }
    public bool IsStepping => IsSteppingInto || IsSteppingOver;
}
