using Modern.Vice.PdbMonitor.Engine.Models;
using Righthand.ViceMonitor.Bridge.Commands;

namespace Righthand.ViceMonitor.Bridge.Services.Abstract;

public static class ViceBridgeTranslations
{
    public static CpuOperation ToCpuOperation(this BreakpointMode source)
    {
        return source switch
        {
            BreakpointMode.Exec => CpuOperation.Exec,
            BreakpointMode.Load => CpuOperation.Load,
            BreakpointMode.Store => CpuOperation.Store,
            _ => throw new ArgumentException($"Unknown {nameof(BreakpointMode)} {source}", nameof(source)),
        };
    }
    public static BreakpointMode ToBreakpointMode(this CpuOperation source)
    {
        return source switch
        {
            CpuOperation.Exec => BreakpointMode.Exec,
            CpuOperation.Load => BreakpointMode.Load,
            CpuOperation.Store => BreakpointMode.Store,
            _ => throw new ArgumentException($"Unknown {nameof(CpuOperation)} {source}", nameof(source)),
        };
    }
}
