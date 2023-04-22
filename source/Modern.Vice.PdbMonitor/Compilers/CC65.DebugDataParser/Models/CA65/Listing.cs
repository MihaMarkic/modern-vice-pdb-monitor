using System.Collections.Immutable;

/// Contains types required to parse ca65 listings
namespace CC65.DebugDataParser.Models.CA65;

public  enum ListingLineType
{
    Code = 1,
    Macro = 2,
}

/// <summary>
/// 
/// </summary>
/// <remarks>Listings are generated with -l filename option</remarks>
public record Listing(string Compiler, string MainFile, string CurrentFile, ImmutableArray<ListingLine> Lines);
public record ListingLine(ParsedListingLine Parsed, string Original);
public abstract record ParsedListingLine(string Address, ListingLineType Type);
public record CommentListingLine(string Address, string Text): ParsedListingLine(Address, ListingLineType.Code);
// Options
public abstract record OptionListingLine(string Address, string Name) 
    : ParsedListingLine(Address, ListingLineType.Code);
public record StringOptionListingLine(string Address, string Name, string Value)
    : OptionListingLine(Address, Name);
public record BoolOptionListingLine(string Address, string Name, bool Value) 
    : OptionListingLine(Address, Name);
public record StringArrayOptionListingLine(string Address, string Name, ImmutableArray<string> Value) 
    : OptionListingLine(Address, Name);
public record DataOptionListingLine(string Address, string Name, ImmutableArray<byte> Data)
    : OptionListingLine(Address, Name);
// Macros
public record MacroListingLine(string Address, string Code): ParsedListingLine(Address, ListingLineType.Macro);
public record MacroStartListingLine(string Address, string Code): MacroListingLine(Address, Code);
public record MacroEndListingLine(string Address) : MacroListingLine(Address, "");
// DBG
public abstract record DbgListingLine(string Address): ParsedListingLine(Address, ListingLineType.Code);
public record FileDbgListingLine(string Address, string File, long Start, long End) : DbgListingLine(Address);
// Sym
/// <summary>
/// 
/// </summary>
/// <param name="Address"></param>
/// <param name="Type"></param>
/// <param name="Name"></param>
/// <param name="Zero">String, it's usually "00", no idea yet what it means.</param>
public abstract record SymDbgListingLine(string Address, string Name, string Zero) : DbgListingLine(Address);
public record ExternSymDbgListingLine(string Address, string Name, string Zero, string ExternName) : 
    SymDbgListingLine(Address, Name, Zero);
public record AutoSymDbgListingLine(string Address, string Name, string Zero, int Offset) :
    SymDbgListingLine(Address, Name, Zero);
public record LineDbgListingLine(string Address) : DbgListingLine(Address);
public record ContentLineDbgListingLine(string Address, string File, int Line): LineDbgListingLine(Address);
public record EmptyLineDbgListingLine(string Address) : LineDbgListingLine(Address);
public abstract record FuncDbgListingLine(string Address, string Name, string Zero) : DbgListingLine(Address);
public record ExternFuncDbgListingLine(string Address, string Name, string Zero, string ExternName) :
    FuncDbgListingLine(Address, Name, Zero);
// Meta
public abstract record MetaListingLine(string Address): ParsedListingLine(Address, ListingLineType.Code);
public record SegmentListingLine(string Address, string Name): MetaListingLine(Address);
public record StartProcListingLine(string Address, string Name) : MetaListingLine(Address);
public record EndProcListingLine(string Address) : MetaListingLine(Address);
// Code
public record CodeListingLine(string Address, ImmutableArray<byte?> Instructions, string? Label, string Code)
    : ParsedListingLine(Address, ListingLineType.Code);
public record EmpyCodeListingLine(string Address, ImmutableArray<byte?> Instructions)
    : CodeListingLine(Address, Instructions, null, string.Empty);
