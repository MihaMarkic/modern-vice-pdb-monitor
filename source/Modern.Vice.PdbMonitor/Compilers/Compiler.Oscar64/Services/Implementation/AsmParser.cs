using System.Collections.Immutable;
using System.Globalization;
using Compiler.Oscar64.Models;
using Microsoft.Extensions.Logging;

namespace Compiler.Oscar64.Services.Implementation;
public class AsmParser
{
    readonly ILogger<AsmParser> logger;
    public abstract class Line;
    public class Delimiter : Line;
    public class Function : Line
    {
        public required string Name { get; init; }
        public required string FullName { get; init; }
    }
    public class Variable: Line
    {
        public required string Name { get; init; }
    }
    public class Data : Line
    {
        public required ushort Address { get; init; }
        public required string Text { get; init; }
        public required ImmutableArray<byte> Content { get; init; }
    }
    public class Label : Line
    {
        public required string Name { get; init; }
    }
    public class Source : Line
    {
        public required int LineNumber { get; init; }
        public required string FilePath { get; init; }
    }
    enum ParsingMode
    {
        SeekingFunction,
        CollectingSourceLines,
        CollectingExecutionLines,
    }
    public AsmParser(ILogger<AsmParser> logger)
    {
        this.logger = logger;
    }

    public ImmutableArray<AssemblyFunction> Parse(ImmutableArray<string> sourceLines)
    {
        var lines = ParseLines(sourceLines);
        var mode = ParsingMode.SeekingFunction;
        var functions = ImmutableArray.CreateBuilder<AssemblyFunction>();
        var sources = new List<AssemblySourceLine>();
        var execLines = new List<AssemblyExecutionLine>();
        Source? source = null;
        Function? function = null;
        int i = 0;
        do
        {
            Line currentLine = lines[i];
            switch (mode)
            {
                case ParsingMode.SeekingFunction:
                    switch (currentLine)
                    {
                        case Function f:
                            function = f;
                            mode = ParsingMode.CollectingSourceLines;
                            break;
                    }
                    i++;
                    break;
                case ParsingMode.CollectingSourceLines:
                    switch(currentLine)
                    {
                        case Source s:
                            source = s;
                            mode = ParsingMode.CollectingExecutionLines;
                            break;
                        case Delimiter:
                            functions.Add(CreateAssemblyFunction(function.ValueOrThrow(), sources));
                            function = null;
                            sources.Clear();
                            mode = ParsingMode.SeekingFunction;
                            break;
                    }
                    i++;
                    break;
                case ParsingMode.CollectingExecutionLines:
                    switch (currentLine)
                    {
                        case Data data:
                            execLines.Add(new AssemblyExecutionLine(data.Address, data.Text, data.Content));
                            i++;
                            break;
                        case Source:
                            sources.Add(CreateAssemblySourceLine(source.ValueOrThrow(), execLines));
                            source = null;
                            execLines.Clear();
                            mode = ParsingMode.CollectingSourceLines;
                            break;
                        case Label:
                            // ignore
                            i++;
                            break;
                        case Delimiter:
                            sources.Add(CreateAssemblySourceLine(source.ValueOrThrow(), execLines));
                            source = null;
                            execLines.Clear();
                            functions.Add(CreateAssemblyFunction(function.ValueOrThrow(), sources));
                            function = null;
                            sources.Clear();
                            i++;
                            mode = ParsingMode.SeekingFunction;
                            break;
                        default:
                            // shouldn't happen
                            i++;
                            break;
                    }
                    break;
            }
        } while (i < lines.Length);
        if (function is not null)
        {
            if (source is not null)
            {
                sources.Add(CreateAssemblySourceLine(source, execLines));
            }
            functions.Add(CreateAssemblyFunction(function.ValueOrThrow(), sources));
        }
        return functions.ToImmutable();
    }

    internal AssemblyFunction CreateAssemblyFunction(Function function, IList<AssemblySourceLine> lines)
    {
        return new AssemblyFunction(function.Name, function.FullName, lines.ToImmutableArray());
    }

    internal AssemblySourceLine CreateAssemblySourceLine(Source source, IList<AssemblyExecutionLine> lines)
    {
        return new AssemblySourceLine(source.LineNumber, source.FilePath, lines.ToImmutableArray());
    }

    public ImmutableArray<Line> ParseLines(ImmutableArray<string> sourceLines)
    {
        var builder = ImmutableArray.CreateBuilder<Line>();
        foreach (string sourceLine in sourceLines)
        {
            Line? newLine;
            if (sourceLine.StartsWith('-'))
            {
                newLine = new Delimiter();
            }
            else if (sourceLine.StartsWith('.'))
            {
                newLine = CreateLabelLine(sourceLine);
            }
            else if (sourceLine.StartsWith(';'))
            {
                newLine = CreateSourceLine(sourceLine);
            }
            else if (IsDataLine(sourceLine, out ushort address))
            {
                newLine = CreateDataLine(sourceLine, address);
            }
            else if (IsVariableLine(sourceLine))
            {
                newLine = CreateVariableLine(sourceLine);
            }
            else
            {
                newLine = CreateFunctionLine(sourceLine);
            }
            if (newLine is not null)
            {
                builder.Add(newLine);
            }
        }
        return builder.ToImmutable();
    }

    internal bool IsDataLine(string line, out ushort address)
    {
        if (line.Length > 0
            && ushort.TryParse(line.AsSpan()[0..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort tempAddress)
            && line[4] == ' '
            && line[5] == ':')
        {
            address = tempAddress;
            return true;
        }
        address = 0;
        return false;
    }

    internal bool IsVariableLine(string line) => line.AsSpan().Trim().EndsWith(":");

    internal Variable? CreateVariableLine(string line)
    {
        int indexOfColumn = line.IndexOf(':');
        if (indexOfColumn < 0)
        {
            logger.LogError("Couldn't parse assembly variable line {line}", line);
            return null;
        }
        return new Variable
        { 
            Name = line[0..indexOfColumn] 
        };
    }

    internal Function? CreateFunctionLine(string line)
    {
        int separatorIndex = line.IndexOf(';');

        // last condition checks whether there is anything after semicolon
        if (separatorIndex < 0 || line.Length == separatorIndex + 1)
        {

            logger.LogError("Couldn't parse assembly header line {line}", line);
            return null;
        }
        return new Function
        {
            Name = line.AsSpan()[0..separatorIndex].TrimEnd(": ").ToString(),
            FullName = line.AsSpan()[(separatorIndex + 1)..].Trim().ToString(),
        };
    }

    internal Source? CreateSourceLine(string line)
    {
        var trimmed = line.AsSpan()[1..];
        int separator = trimmed.IndexOf(',');
        if (separator >= 0 && int.TryParse(trimmed[0..separator], out int lineNumber))
        {
            return new Source
            {
                LineNumber = lineNumber,
                FilePath = trimmed[(separator + 1)..].Trim(" \"").ToString(),
            };
        }
        logger.LogError("Couldn't parse assembly source line {line}", line);
        return null;
    }

    internal Label? CreateLabelLine(string line)
    {
        int indexOfColumn = line.IndexOf(':');
        if (indexOfColumn < 0)
        {
            logger.LogError("Can't parse assembly label line: {line}", line);
            return null;
        }
        return new Label { Name = line.Substring(1, indexOfColumn - 1) };
    }

    internal Data CreateDataLine(string line, ushort address)
    {
        var source = line.AsSpan();
        var builder = ImmutableArray.CreateBuilder<byte>(3);
        for (int i = 7; i < 15; i += 3)
        {
            var byteText = source[i..(i + 2)];
            if (MemoryExtensions.Equals(byteText, "__", StringComparison.Ordinal))
            {
                break;
            }
            if (byte.TryParse(byteText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
            {
                builder.Add(b);
            }
            else
            {
                logger.LogError("Failed to parse assembly execution byte {Data}", byteText.ToString());
            }
        }
        return new Data
        {
            Address = address,
            Text = source[16..].Trim().ToString(),
            Content = builder.ToImmutableArray()
        };
    }
}
