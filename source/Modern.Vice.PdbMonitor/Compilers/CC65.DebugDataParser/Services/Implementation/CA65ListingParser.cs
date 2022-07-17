using System.Collections.Immutable;
using System.Globalization;
using CC65.DebugDataParser.Models.CA65;

namespace CC65.DebugDataParser.Services.Implementation;
public class CA65ListingParser
{
    internal readonly record struct ParserState(bool IsMacro);
    public readonly record struct LineInfo(string Address, ListingLineType Type, ImmutableArray<byte?> Data);
    public async Task<Listing> ParseAsync(TextReader reader, CancellationToken ct)
    {
        var sourceLines = new List<string>();
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                sourceLines.Add(line);
            }
        }
        var listingLines = new List<ListingLine>(sourceLines.Count);
        var state = new ParserState(false);
        foreach (string line in sourceLines)
        {
            var parsed = ParseLine(state, line);
            switch (parsed)
            {
                case MacroStartListingLine:
                    if (state.IsMacro)
                    {
                        throw new Exception("Invalid state while reading macro code");
                    }
                    state = state with { IsMacro = true };
                    break;
                case MacroEndListingLine:
                    if (!state.IsMacro)
                    {
                        throw new Exception("Invalid state while reading macro code");
                    }
                    state = state with { IsMacro = false };
                    break;
            }
            listingLines.Add(new ListingLine(parsed, line));
        }
        return new Listing("", "", "", listingLines.ToImmutableArray());
    }
    internal ListingLineType ConvertToListingLineType(char text)
    {
        return text switch
        {
            '1' => ListingLineType.Code,
            '2' => ListingLineType.Macro,
            _ => throw new Exception($"Unknown line type '{text}'"),
        };
    }
    internal ImmutableArray<byte?> ConvertToData(ReadOnlySpan<char> text)
    {
        // TODO use ReadOnlySpan<char>.Split when available
        var parts = text.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return ImmutableArray<byte?>.Empty;
        }
        return parts.Select(p => byte.TryParse(p, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte result) ? (byte?)result : null).ToImmutableArray();
    }
    internal LineInfo ExtractAddressAndType(ReadOnlySpan<char> text)
    {
        const int RequiredTextLength = 24;
        if (text.Length != RequiredTextLength)
        {
            throw new ArgumentException($"Text should be exactly {RequiredTextLength} chars long", nameof(text));
        }
        return new LineInfo(text[0..7].ToString(), ConvertToListingLineType(text[8]), ConvertToData(text[9..]));

    }
    internal ParsedListingLine ParseLine(ParserState state, string line)
    {
        var span = line.AsSpan();
        var info = ExtractAddressAndType(span[0..24]);
        var contentText = span[24..];
        if (state.IsMacro)
        {
            return ParseMacro(info, contentText);
        }
        else
        {
            if (contentText.StartsWith(".macro"))
            {
                return ParseMacro(info, contentText);
            }
            else if (contentText.StartsWith(";"))
            {
                return ParseComment(info, contentText);
            }
            else if (contentText.StartsWith("\t"))
            {
                var text = contentText[1..];
                if (text.StartsWith("."))
                {
                    return ParseOption(info, contentText[2..]);
                }
                else
                {
                    return ParseCode(info, text[1..]);
                }
            }
            else if (contentText.StartsWith("."))
            {
                return ParseMeta(info, contentText[1..]);
            }
        }
        throw new NotImplementedException();
    }
    // Comment
    internal CommentListingLine ParseComment(LineInfo info, ReadOnlySpan<char> line)
    {
        string text = line.Length > 2 ? line[2..].ToString() : "";
        return new CommentListingLine(info.Address, text);
    }
    // Options
    internal ParsedListingLine ParseOption(LineInfo info, ReadOnlySpan<char> text)
    {
        int firstTab = text.IndexOf('\t');
        if (firstTab < 0)
        {
            throw new Exception($"Couldn't extract option name from {text} - no end tab char");
        }
        int lastTab = text.LastIndexOf('\t');
        var name = text[firstTab..lastTab].Trim().ToString();
        if (!name.StartsWith('.'))
        {
            throw new Exception($"Option name '{name}' should start with .");
        }
        name = name[1..];
        var content = text[(lastTab + 1)..];
        switch (name)
        {
            case "fopt":
            case "setcpu":
            case "macpack":
            case "forceimport":
            case "import":
            case "export":
                {
                    string value = ParseStringOptionValue(content);
                    return new StringOptionListingLine(info.Address, name, value);
                }
            case "smart":
            case "autoimport":
            case "case":
            case "debuginfo":
                {
                    bool value = ParseBoolOptionValue(content);
                    return new BoolOptionListingLine(info.Address, name, value);
                }
            case "importzp":
            // temporarily maps to strings, not data
            case "byte":
                {
                    var array = ParseStringArrayOptionValue(content);
                    return new StringArrayOptionListingLine(info.Address, name, array);
                }
            case "dbg":
                return ParseDbg(info, content);
            default:
                throw new Exception($"Couldn't parse option line '{text}'");
        }
    }
    internal string ParseStringOptionValue(ReadOnlySpan<char> value) => value.ToString();
    internal bool ParseBoolOptionValue(ReadOnlySpan<char> value) => value.Equals("on", StringComparison.Ordinal);
    internal ImmutableArray<string> ParseStringArrayOptionValue(ReadOnlySpan<char> value)
    {
        // TODO use ReadOnlySpan<char>.Split when available
        var parts = value.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Select(p => p.Trim('\"')).ToImmutableArray();
    }
    // Macro
    internal MacroListingLine ParseMacro(LineInfo info, ReadOnlySpan<char> line)
    {
        if (line.StartsWith(".endmacro", StringComparison.Ordinal))
        {
            return new MacroEndListingLine(info.Address);
        }
        else if (line.StartsWith(".macro"))
        {
            return new MacroStartListingLine(info.Address, line[".macro".Length..].Trim().ToString());
        }
        else
        {
            return new MacroListingLine(info.Address, line.Trim().ToString());
        }
    }
    // DBG
    internal DbgListingLine ParseDbg(LineInfo info, ReadOnlySpan<char> line)
    {
        throw new NotImplementedException();
    }
    internal FileDbgListingLine ParseFileDbg(LineInfo info, ReadOnlySpan<char> line)
    {
        var parts = line.ToString().Split(',', StringSplitOptions.TrimEntries);
        return new FileDbgListingLine(info.Address, parts[0].Trim('\"'), long.Parse(parts[1]), long.Parse(parts[2]));
    }
    internal SymDbgListingLine ParseSymDbg(LineInfo info, ReadOnlySpan<char> line)
    {
        var parts = line.ToString().Split(',', StringSplitOptions.TrimEntries);
        return parts[2] switch
        {
            "extern" => new ExternSymDbgListingLine(info.Address, parts[0].Trim('\"'), parts[1].Trim('\"'), parts[3].Trim('\"')),
            "auto" => new AutoSymDbgListingLine(info.Address, parts[0].Trim('\"'), parts[1].Trim('\"'), int.Parse(parts[3])),
            _ => throw new Exception($"Unknown symbol '{line}'")
        };
    }
    internal LineDbgListingLine ParseLineDbg(LineInfo info, ReadOnlySpan<char> line)
    {
        if (line.Length > 0)
        {
            var parts = line.ToString().Split(',', StringSplitOptions.TrimEntries);
            return new ContentLineDbgListingLine(info.Address, parts[0].Trim('\"'), int.Parse(parts[1]));
        }
        else
        {
            return new EmptyLineDbgListingLine(info.Address);
        }
    }
    internal FuncDbgListingLine ParseFuncDbg(LineInfo info, ReadOnlySpan<char> line)
    {
        var parts = line.ToString().Split(',', StringSplitOptions.TrimEntries);
        if (!parts[2].Equals("extern",  StringComparison.Ordinal))
        {
            throw new Exception($"Non external .dbg functions are not supported");
        }
        return new ExternFuncDbgListingLine(info.Address, parts[0].Trim('\"'), parts[1].Trim('\"'), parts[3].Trim('\"'));
    }
    // Meta
    internal MetaListingLine ParseMeta(LineInfo info, ReadOnlySpan<char> line)
    {
        var parts = line.ToString().Split('\t', StringSplitOptions.TrimEntries);
        return parts[0] switch
        {
            "segment" => new SegmentListingLine(info.Address, parts[1].Trim('\"')),
            "proc" => new StartProcListingLine(info.Address, line["proc".Length..].Trim().ToString()),
            "endproc" => new EndProcListingLine(info.Address),
            _ => throw new Exception($"Unsupported meta {parts[0]}"),
        };
    }
    internal CodeListingLine ParseCode(LineInfo info, ReadOnlySpan<char> line)
    {
        var parts = line.ToString().Split('\t', StringSplitOptions.TrimEntries);
        string? label;
        string content;
        if (parts[0].EndsWith(':'))
        {
            label = parts[0][0..^1].ToString();
            content = line[parts[0].Length..].ToString();
        }
        else
        {
            label = null;
            content = line.ToString();
        }
        return new CodeListingLine(info.Address, info.Data, label, content);
    }
}
