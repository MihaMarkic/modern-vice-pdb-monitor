using System.Collections.Immutable;

namespace Modern.Vice.PdbMonitor.Engine.Models
{
    /// <summary>
    /// Contains all debugging information
    /// </summary>
    public record AcmePdb(ImmutableArray<AcmeLine> Lines, ImmutableDictionary<string, AcmeFile> Files, ImmutableDictionary<string, AcmeLabel> Labels,
        ImmutableArray<AcmeLine> LinesWithAddress)
    {
        public static AcmePdb Empty { get; } = new AcmePdb(ImmutableArray<AcmeLine>.Empty, ImmutableDictionary<string, AcmeFile>.Empty, 
            ImmutableDictionary<string, AcmeLabel>.Empty, ImmutableArray<AcmeLine>.Empty);
    }
    public record AcmeFile(string RelativePath, ImmutableArray<AcmeLine> Lines)
    {
        public AcmeFile(string relativePath) : this(relativePath, ImmutableArray<AcmeLine>.Empty)
        { }
    }
    /// <summary>
    /// DataLength might be longer than data where there are more than 8 bytes (ACME report omits next bytes)
    /// </summary>
    public record AcmeLine(string FileRelativePath, int LineNumber, ushort? StartAddress,
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
    public record AcmeLabel(ushort Address, string Name);

    public record AcmePdbParseResult<T>(T ParsedData, ImmutableArray<AcmePdbParseError> Errors);
    public static class AcmePdbParseResultBuilder
    {
        public static AcmePdbParseResult<T> Create<T>(T parsedData, ImmutableArray<AcmePdbParseError> errors) => new AcmePdbParseResult<T>(parsedData, errors);
    }
    public record AcmePdbParseError(int LineNumber, string Line, string ErrorText);
}
