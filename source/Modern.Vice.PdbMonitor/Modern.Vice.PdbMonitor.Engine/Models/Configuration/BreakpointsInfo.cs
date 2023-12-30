using System.Text.Json.Serialization;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Engine.Models.Configuration;
public record BreakpointsSettings(ImmutableArray<BreakpointInfo> Breakpoints)
{
    public static BreakpointsSettings Empty = new (ImmutableArray<BreakpointInfo>.Empty);
}
public record BreakpointInfo(bool StopWhenHit, bool IsEnabled, BreakpointMode Mode, 
    string? Condition, BreakpointInfoBind Bind);

[JsonDerivedType(typeof(BreakpointInfoLineBind), typeDiscriminator: "line")]
[JsonDerivedType(typeof(BreakpointInfoLabelBind), typeDiscriminator: "label")]
[JsonDerivedType(typeof(BreakpointInfoGlobalVariableBind), typeDiscriminator: "global_variable")]
[JsonDerivedType(typeof(BreakpointInfoNoBind), typeDiscriminator: "unbound")]
public abstract record BreakpointInfoBind;
public record BreakpointInfoLineBind(PdbPath FilePath, int LineNumber, string LineText): BreakpointInfoBind;
public record BreakpointInfoLabelBind(PdbLabel Label): BreakpointInfoBind;
public record BreakpointInfoGlobalVariableBind(string VariableName): BreakpointInfoBind;
public record BreakpointInfoNoBind(ushort StartAddress, ushort EndAddress): BreakpointInfoBind;

public static class BreakpointInfoBindingExtensions
{
    public static BreakpointInfoBind ConvertFromModel(this BreakpointBind bind)
    {
        return bind switch
        {
            BreakpointLineBind lineBind => lineBind.ConvertFromModel(),
            BreakpointLabelBind label => label.ConvertFromModel(),
            BreakpointGlobalVariableBind globalVariable => globalVariable.ConvertFromModel(),
            BreakpointNoBind noBind => noBind.ConvertFromModel(),
            _ => throw new ArgumentOutOfRangeException(nameof(bind)),
        };
    }
    public static BreakpointInfoBind ConvertFromModel(this BreakpointLineBind bind) 
        => new BreakpointInfoLineBind(bind.File.Path, bind.LineNumber, bind.Line.Text);
    public static BreakpointInfoLabelBind ConvertFromModel(this BreakpointLabelBind bind)
        => new BreakpointInfoLabelBind(bind.Label);
    public static BreakpointInfoGlobalVariableBind ConvertFromModel(this BreakpointGlobalVariableBind bind)
    => new BreakpointInfoGlobalVariableBind(bind.VariableName);
    public static BreakpointInfoNoBind ConvertFromModel(this BreakpointNoBind bind)
=> new BreakpointInfoNoBind(bind.StartAddress, bind.EndAddress);
}
