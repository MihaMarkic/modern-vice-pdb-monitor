using System.ComponentModel.DataAnnotations;

namespace Modern.Vice.PdbMonitor.Engine.Models;

public enum DebugAutoStartMode
{
    [Display(Description = "No auto start")]
    None,
    [Display(Description = "Auto starts using VICE")]
    Vice,
    [Display(Description = "Auto starts at 'start' label address")]
    AtAddress,
}

public enum BreakpointMode
{
    Exec,
    Load,
    Store
}

[Flags]
public enum DialogButton
{
    OK,
    Cancel
}

public enum DialogResultCode
{
    OK,
    Cancel
}
