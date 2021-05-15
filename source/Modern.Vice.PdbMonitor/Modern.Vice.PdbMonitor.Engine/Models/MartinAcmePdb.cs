//using System.Collections.Immutable;

//namespace Modern.Vice.PdbMonitor.Engine.Models
//{
//    public record AcmePdb(
//        ImmutableArray<AcmePdbInclude> Includes,
//        ImmutableArray<AcmePdbFile> Files,
//        ImmutableArray<AcmePdbAddress> Addresses,
//        ImmutableArray<AcmePdbLabel> Labels)
//    {
//        public static AcmePdb Empty { get; } = new AcmePdb(ImmutableArray<AcmePdbInclude>.Empty, ImmutableArray<AcmePdbFile>.Empty,
//            ImmutableArray<AcmePdbAddress>.Empty, ImmutableArray<AcmePdbLabel>.Empty);
//    }

//    public record AcmePdbInclude(string Path);
//    public record AcmePdbFile(int Index, string Path);
//    public record AcmePdbAddress(int Address, int Zone, int FileIndex, int Line);
//    public record AcmePdbLabel(int Address, int Zone, string Name, bool Used, bool Memory);

//    public record AcmePdbParseResult(AcmePdb AcmePdb, ImmutableArray<AcmePdbParseError> Errors);
//    public record AcmePdbParseError(int LineNumber, string Line, string ErrorText);

//}
