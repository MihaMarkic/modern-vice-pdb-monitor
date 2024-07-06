using System.Collections.Immutable;
using System.Xml.Linq;
using Assembler.KickAssembler.Models;
using Microsoft.Extensions.Logging;

namespace Assembler.KickAssembler.Services.Implementation;

public class KickAssemblerDbgParser
{
    private readonly ILogger<KickAssemblerDbgParser> _logger;
    public KickAssemblerDbgParser(ILogger<KickAssemblerDbgParser> logger)
    {
        _logger = logger;
    }
    public async Task<C64Debugger> LoadFileAsync(string path, CancellationToken ct = default)
    {
        C64Debugger? result = null;
        if (!File.Exists(path))
        {
            throw new Exception($"File {path} does not exist");
        }
        string content;
        try
        {
            content = await File.ReadAllTextAsync(path, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load KickAssembler debug files {path}");
            throw;
        }
        result = await LoadContentAsync(content, ct);
        return result;
    }

    internal XElement GetElement(XElement root, string name) => root.Element(name) ?? new XElement(name);
    internal async ValueTask<C64Debugger> LoadContentAsync(string content, CancellationToken ct = default)
    {
        const string rootName = "C64debugger";
        try
        {
            var root = XElement.Parse(content);
            if (!rootName.Equals(root.Name.LocalName, StringComparison.Ordinal))
            {
                throw new Exception($"Excepted root element {rootName} but was {root.Name.LocalName}");
            }
            var source = GetElement(root,"Sources");
            var sourcesTask = ParseSources(source, ct);
            var segments = GetElement(root,"Segments");
            return new C64Debugger(
                (string?)root.Attribute("Version") ?? "?",
                await sourcesTask,
                ImmutableArray<Segment>.Empty,
                ImmutableArray<Label>.Empty,
                ImmutableArray<Breakpoint>.Empty,
                ImmutableArray<Watchpoint>.Empty
                );
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to parser KickAssembler debug content");
            throw;
        }
    }

    internal int CountChars(string text, char c)
    {
        int count = 0;
        for (int i=0; i<text.Length; i++)
        {
            if (text[i] == c)
            {
                count++;
            }
        }
        return count;
    }
    internal ValueTask<ImmutableArray<Source>> ParseSources(XElement sources, CancellationToken ct = default)
    {
        string lines = sources.Value;
        var builder = ImmutableArray.CreateBuilder<Source>(CountChars(lines, '\n') + 1);
        using (var reader = new StringReader(lines))
        {
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                builder.Add(ParseSource(line));
            }
        }
        return new ValueTask<ImmutableArray<Source>>(builder.ToImmutable());
    }

    internal Source ParseSource(string line)
    {
        var parts = line.Trim().Split(',');
        if (parts.Length != 2)
        {
            throw new Exception($"Source line '{line}' should have two parts separated by comma");
        }
        if (!int.TryParse(parts[0], out int index))
        {
            throw new Exception($"Source line '{line}' should have a valid number as first comma separated value");
        }
        const string kickAssJar = "KickAss.jar:";
        if (parts[1].StartsWith(kickAssJar, StringComparison.Ordinal))
        {
            return new Source(index, SourceOrigin.KickAss, parts[1].Substring((kickAssJar.Length)));
        }
        else
        {
            return new Source(index, SourceOrigin.User, parts[1]);
        }
    }
}
