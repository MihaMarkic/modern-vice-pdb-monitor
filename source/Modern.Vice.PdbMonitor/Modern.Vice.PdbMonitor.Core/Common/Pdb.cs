using System;
using System.Collections.Immutable;
using System.Linq;

namespace Modern.Vice.PdbMonitor.Core.Common;

/// <summary>
/// Contains all debugging information
/// </summary>
public record Pdb(ImmutableArray<PdbLine> Lines, ImmutableDictionary<string, PdbFile> Files, ImmutableDictionary<string, PdbLabel> Labels,
    ImmutableArray<PdbLine> LinesWithAddress)
{
    public static Pdb Empty { get; } = new Pdb(ImmutableArray<PdbLine>.Empty, ImmutableDictionary<string, PdbFile>.Empty,
        ImmutableDictionary<string, PdbLabel>.Empty, ImmutableArray<PdbLine>.Empty);
}
public record PdbFile(string RelativePath, ImmutableArray<PdbLine> Lines)
{
    public PdbFile(string relativePath) : this(relativePath, ImmutableArray<PdbLine>.Empty)
    { }
    // TODO improve string creation each time this line is called
    public string Content => string.Join(Environment.NewLine, Lines.Select(l => l.Text));
}
/// <summary>
/// DataLength might be longer than data where there are more than 8 bytes (ACME report omits next bytes)
/// </summary>
public record PdbLine(string FileRelativePath, int LineNumber, ushort? StartAddress,
    ImmutableArray<byte>? Data, ushort DataLength, bool? HasMoreData, string Text)
{
    public ushort? EndAddress => StartAddress.HasValue ? (ushort)(StartAddress.Value + DataLength - 1) : null;
    public bool IsAddressWithinLine(ushort address)
    {
        if (StartAddress.HasValue)
        {
            return address >= StartAddress.Value && address <= EndAddress;
        }
        return false;
    }
}
public record PdbLabel(ushort Address, string Name);

public record PdbParseResult<T>(T ParsedData, ImmutableArray<PdbParseError> Errors);
public static class PdbParseResultBuilder
{
    public static PdbParseResult<T> Create<T>(T parsedData, ImmutableArray<PdbParseError> errors) => new PdbParseResult<T>(parsedData, errors);
}
public record PdbParseError(int LineNumber, string Line, string ErrorText);
