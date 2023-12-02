using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Engine.Models.Configuration;
public record BreakpointsSettings(ImmutableArray<BreakpointInfo> Breakpoints)
{
    public static BreakpointsSettings Empty = new BreakpointsSettings(ImmutableArray<BreakpointInfo>.Empty);
}
public record BreakpointInfo(bool StopWhenHit, bool IsEnabled, BreakpointMode Mode, 
    ushort StartAddress, ushort EndAddress, string? Condition, 
    PdbPath? FilePath, int? LineNumber, string? Text, PdbLabel? Label);
