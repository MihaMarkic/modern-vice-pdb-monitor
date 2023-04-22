using System.Collections.Immutable;
using CC65.DebugDataParser.Models.CL65;

namespace CC65.DebugDataParser.Services.Implementation;
public class CL65DebugSymbolsParser
{
    static readonly char[] BlankOrTab = new char[] { '\t', ' ' };
    static readonly string[] LineTypes = new string[] { "version", "info", "csym", "file", "lib", "line", "mod", "seg",
    "span", "scope","sym", "type" };
    //public async Task<Listing> ParseAsync(TextReader reader, CancellationToken ct)
    //{
    //}

    internal CoreLine ParseLine(ReadOnlySpan<char> line)
    {
        if (line.Length < 2 || !char.IsLower(line[0]))
        {
            throw new Exception($"Invalid debug symbols line: '{line}'");
        }
        var content = GetLineContent(line);
        var data = Split(content);
        var pairs = CreateKeyValuePairs(content, data);
        if (line.StartsWith("version"))
        {
            return ParseVersion(pairs);
        }
        else if (line.StartsWith("info"))
        {
            return ParseInfo(pairs);
        }
        else if (line.StartsWith("csym"))
        {
            return ParseCSym(pairs);
        }
        else if (line.StartsWith("file"))
        {
            return ParseFile(pairs);
        }
        else if (line.StartsWith("lib"))
        {
            return ParseLib(pairs);
        }
        else if (line.StartsWith("line"))
        {
            return ParseLine(pairs);
        }
        else if (line.StartsWith("mod"))
        {
            return ParseMod(pairs);
        }
        else if (line.StartsWith("seg"))
        {
            return ParseSeg(pairs);
        }
        else if (line.StartsWith("span"))
        {
            return ParseSpan(pairs);
        }
        else if (line.StartsWith("scope"))
        {
            return ParseScope(pairs);
        }
        else if (line.StartsWith("sym"))
        {
            return ParseSym(pairs);
        }
        else if (line.StartsWith("type"))
        {
            return ParseType(pairs);
        }
        else
        {
            throw new Exception($"Unrecognized debug symbols line type '{line}'");
        }
        throw new NotImplementedException();
    }

    internal Sc ParseSc(string value) => value switch
    {
        "ext" => Sc.Ext,
        _ => throw new ArgumentException($"Invalid value '{value}' for Sc enum"),
    };
    internal AddrSize ParseAddrSize(string value) => value switch
    {
        "absolute" => AddrSize.Absolute,
        "zeropage" => AddrSize.ZeroPage,
        _ => throw new ArgumentException($"Invalid value '{value}' for AddrSize enum"),
    };

    internal SegType ParseSegType(string value) => value switch
    {
        "ro" => SegType.Ro,
        "rw" => SegType.Rw,
        _ => throw new ArgumentException($"Invalid value '{value}' for SegType enum"),
    };
    internal SymType ParseSymType(string value) => value switch
    {
        "lab" => SymType.Lab,
        "imp" => SymType.Imp,
        "equ" => SymType.Equ,
        _ => throw new ArgumentException($"Invalid value '{value}' for SymType enum"),
    };
    internal ScopeType ParseScopeType(string value) => value switch
    {
        "scope" => ScopeType.Scope,
        _ => throw new ArgumentException($"Invalid value '{value}' for ScopeType enum"),
    };
    /// <summary>
    /// Skips line type and trims the content.
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    ReadOnlySpan<char>  GetLineContent(ReadOnlySpan<char> line)
    {
        int i = 1;
        while (!char.IsWhiteSpace(line[i]))
        {
            i++;
        }
        return line[i..].Trim();
    }

    internal VersionLine ParseVersion(ImmutableDictionary<string, string> values) =>
        new VersionLine(int.Parse(values["major"]), int.Parse(values["minor"]));
    internal InfoLine ParseInfo(ImmutableDictionary<string, string> values) =>
        new InfoLine(
            int.Parse(values["csym"]),
            int.Parse(values["file"]),
            int.Parse(values["lib"]),
            int.Parse(values["line"]),
            int.Parse(values["mod"]),
            int.Parse(values["scope"]),
            int.Parse(values["seg"]),
            int.Parse(values["span"]),
            int.Parse(values["sym"]),
            int.Parse(values["type"]));
    internal CSymLine ParseCSym(ImmutableDictionary<string, string> values) =>
        new CSymLine(int.Parse(values["id"]), 
            values["name"].Trim('"'),
            int.Parse(values["scope"]),
            int.Parse(values["type"]),
            ParseSc(values["sc"]),
            int.Parse(values["sym"]));
    internal FileLine ParseFile(ImmutableDictionary<string, string> values) =>
        new FileLine(int.Parse(values["id"]),
            values["name"].Trim('"'),
            int.Parse(values["size"]),
            int.Parse(values["mtime"].AsSpan()[2..], System.Globalization.NumberStyles.HexNumber),
            int.Parse(values["mod"]));
    internal LibLine ParseLib(ImmutableDictionary<string, string> values) =>
        new LibLine(
            int.Parse(values["id"]), 
            values["name"].Trim('"'));
    internal LineLine ParseLine(ImmutableDictionary<string, string> values) =>
        new LineLine(
            int.Parse(values["id"]),
            int.Parse(values["file"]),
            int.Parse(values["line"]),
            ParseNullableInt(values, "type"),
            ParseNullableInt(values, "span"),
            ParseNullableInt(values, "count")
            );
    internal ModLine ParseMod(ImmutableDictionary<string, string> values) =>
        new ModLine(
            int.Parse(values["id"]),
            values["name"].Trim('"'),
            int.Parse(values["file"]),
            ParseNullableInt(values, "lib"));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    /// <example>seg	id=0,name="CODE",start=0x000840,size=0x0874,addrsize=absolute,type=ro,oname="a.out",ooffs=65</example>
    internal SegLine ParseSeg(ImmutableDictionary<string, string> values) =>
        new SegLine(
            int.Parse(values["id"]),
            values["name"].Trim('"'),
            int.Parse(values["start"].AsSpan()[2..], System.Globalization.NumberStyles.HexNumber),
            int.Parse(values["size"].AsSpan()[2..], System.Globalization.NumberStyles.HexNumber),
            ParseAddrSize(values["addrsize"]),
            ParseSegType(values["type"]),
            ParseNullableString(values, "oname"),
            ParseNullableInt(values, "ooffs"));
    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <example>span	id=0,seg=1,start=0,size=4,type=1</example>
    internal SpanLine ParseSpan(ImmutableDictionary<string, string> values) =>
        new SpanLine(
            int.Parse(values["id"]),
            int.Parse(values["seg"]),
            int.Parse(values["start"]),
            int.Parse(values["size"]),
            ParseNullableInt(values, "type")
            );
    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    /// <example>scope	id=1,name="_main",mod=0,type=scope,size=27,parent=0,sym=3,span=15</example>
    internal ScopeLine ParseScope(ImmutableDictionary<string, string> values) =>
        new ScopeLine(
            int.Parse(values["id"]),
            values["name"].Trim('"'),
            int.Parse(values["mod"]),
            int.Parse(values["size"]),
            int.Parse(values["span"]),
            ParseNullableEnum(values, "type", ParseScopeType),
            ParseNullableInt(values, "parent"),
            ParseNullableInt(values, "sym")
            );
    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    /// <example>sym	id=0,name="L0001",addrsize=absolute,size=1,scope=1,def=10+18,ref=13,val=0x85A,seg=0,type=lab</example>
    internal SymLine ParseSym(ImmutableDictionary<string, string> values) =>
        new SymLine(
            int.Parse(values["id"]),
            values["name"].Trim('"'),
            ParseAddrSize(values["addrsize"]),
            int.Parse(values["scope"]),
            values["def"],
            values["ref"],
            ParseSymType(values["type"]),
            ParseNullableInt(values, "size"),
            ParseNullableHexInt(values, "val"),
            ParseNullableInt(values, "seg"),
            ParseNullableInt(values, "exp"));
    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    /// <example>type	id=1,val="800420"</example>
    internal TypeLine ParseType(ImmutableDictionary<string, string> values) =>
        new TypeLine(
            int.Parse(values["id"]),
            values["val"].Trim('"'));
    int ? ParseNullableInt(ImmutableDictionary<string, string> values, string key)
    {
        if (values.TryGetValue(key, out var value))
        {
            return int.Parse(value);
        }
        return null;
    }
    int? ParseNullableHexInt(ImmutableDictionary<string, string> values, string key)
    {
        if (values.TryGetValue(key, out var value))
        {
            return int.Parse(value.AsSpan()[2..], System.Globalization.NumberStyles.HexNumber);
        }
        return null;
    }
    string? ParseNullableString(ImmutableDictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value) ? value.Trim('"') : null;
    T? ParseNullableEnum<T>(ImmutableDictionary<string, string> values, string key, Func<string, T> parser)
        where T: struct, Enum
    {
        if (values.TryGetValue(key, out var value))
        {
            return parser(value);
        }
        return null;
    }
    internal ImmutableArray<(int Start, int End)> Split(ReadOnlySpan<char> text, char separator = ',')
    {
        var result = ImmutableArray<(int Start, int End)>.Empty;
        if (text.Length > 0)
        {
            int start = 0;
            int end = 1;
            char c;
            do
            {
                while (end < text.Length && (c = text[end]) != separator)
                {
                    end++;
                }
                result = result.Add((start, end));
                start = end + 1;
                end = start;
            } while (end < text.Length);
        }
        return result;
    }

    internal ImmutableDictionary<string, string> CreateKeyValuePairs(ReadOnlySpan<char> content, ImmutableArray<(int Start, int End)> source)
    {
        var result = ImmutableDictionary<string, string>.Empty;
        foreach (var p in source)
        {
            var pair = content[p.Start..p.End];
            var parts = Split(pair, '=');
            if (parts.Length != 2)
            {
                throw new Exception($"Failed to parse key value pair '{pair}'");
            }
            string key = pair[parts[0].Start..parts[0].End].ToString();
            string value = pair[parts[1].Start..parts[1].End].ToString();
            result = result.Add(key, value);
        }
        return result;
    }
}
