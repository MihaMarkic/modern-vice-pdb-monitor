namespace CC65.DebugDataParser.Models.CL65;
public enum Sc
{
    Ext
}
public enum AddrSize
{
    Absolute,
    ZeroPage,
}
public enum SegType
{
    Ro,
    Rw,
}
public enum SymType
{
    Lab,
    Imp,
    Equ,
}
public enum ScopeType
{
    Scope,
}
public abstract record CoreLine;
public record VersionLine(int Major, int Minor) : CoreLine;
public record InfoLine(int CSym, int File, int Lib, int Line, int Mod, int Scope, int Seg, int Span, int Sym, int Type) : CoreLine;
public record CSymLine(int Id, string Name, int Scope, int Type, Sc Sc, int Sym) : CoreLine;
public record FileLine(int Id, string Name, int Size, long MTime, int Mod) : CoreLine;
public record LibLine(int Id, string Name) : CoreLine;
public record LineLine(int Id, int File, int Line, int? Type = null, int? Span = null, int? Count = null) : CoreLine;
public record ModLine(int Id, string Name, int File, int? Lib = null) : CoreLine;
public record SegLine(int Id, string Name, long Start, long Size, AddrSize AddrSize, SegType SegType, string? OName = null, int? Ooffs = null) 
    : CoreLine;
public record SpanLine(int Id, int Seg, int Start, int Size, int? Type = null) : CoreLine;
public record ScopeLine(int Id, string Name, int Mod, int Size, int Span, ScopeType? ScopeType = null, int? Parent = null, int? Sym = null) 
    : CoreLine;
public record SymLine(int Id, string Name, AddrSize AddrSize, int Scope, string Def,
    string Ref, SymType Type, int? Size = null, int? Val = null, int? Seg = null, int? Exp = null) : CoreLine;
public record TypeLine(int Id, string Val) : CoreLine;
