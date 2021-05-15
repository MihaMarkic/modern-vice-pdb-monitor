using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Modern.Vice.PdbMonitor.Engine.Models
{
    /// <summary>
    /// Contains all debugging information
    /// </summary>
    public record AcmePdb(ImmutableArray<AcmeLine> Lines, ImmutableDictionary<string, AcmeFile> Files, ImmutableDictionary<string, AcmeLabel> Labels)
    {
        public static AcmePdb Empty { get; } = new AcmePdb(ImmutableArray<AcmeLine>.Empty, ImmutableDictionary<string, AcmeFile>.Empty, ImmutableDictionary<string, AcmeLabel>.Empty);
    }
    public record AcmeFile(string RelativePath, ImmutableArray<AcmeLine> Lines)
    {
        public AcmeFile(string relativePath) : this(relativePath, ImmutableArray<AcmeLine>.Empty)
        { }
    }
    public record AcmeLine(AcmeFile File, int LineNumber, ushort? StartAddress, ImmutableArray<byte>? Data, bool? IsMoreData, string Text);
    public record AcmeLabel(ushort Address, string Name);

    public record AcmePdbParseResult<T>(T ParsedData, ImmutableArray<AcmePdbParseError> Errors);
    public static class AcmePdbParseResultBuilder
    {
        public static AcmePdbParseResult<T> Create<T>(T parsedData, ImmutableArray<AcmePdbParseError> errors) => new AcmePdbParseResult<T>(parsedData, errors);
    }
    public record AcmePdbParseError(int LineNumber, string Line, string ErrorText);
}
