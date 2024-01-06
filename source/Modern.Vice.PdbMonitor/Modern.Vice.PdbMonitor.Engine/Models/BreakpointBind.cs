using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Engine.Models;
public abstract record BreakpointBind;
public record BreakpointLineBind(PdbFile File, PdbLine Line, int LineNumber) : BreakpointBind
{
    public PdbPath? FileName => File.Path;
    /// <summary>
    /// Shows info.
    /// </summary>
    /// <returns></returns>
    /// <remarks>LineNumber is Editor adjusted (+1).</remarks>
    public override string ToString() => $"Line {LineNumber+1} File {File.Path.Path}";
}
public record BreakpointGlobalVariableBind(string VariableName) : BreakpointBind
{
    public override string ToString() => $"Global Variable {VariableName}";
}
public record BreakpointLabelBind(PdbLabel Label) : BreakpointBind
{
    public override string ToString() => $"Label {Label.Name}";
}
public record BreakpointNoBind(ushort StartAddress, ushort EndAddress) : BreakpointBind
{
    public static BreakpointNoBind Empty { get; } = new BreakpointNoBind(0, 0);
    public override string ToString() => "Unbound";
}
