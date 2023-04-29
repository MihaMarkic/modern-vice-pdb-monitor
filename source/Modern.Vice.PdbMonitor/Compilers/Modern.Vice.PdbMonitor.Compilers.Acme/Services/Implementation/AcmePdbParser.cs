using Modern.Vice.PdbMonitor.Compilers.Acme.Services.Abstract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Core.Common.Compiler;

namespace Modern.Vice.PdbMonitor.Compilers.Acme.Services.Implementation;

public class AcmePdbParser : IAcmePdbParser
{
    public interface IContext
    {
        abstract void AddError(PdbParseError error);
        abstract ImmutableArray<PdbParseError> Errors { get; }
    }
    public class Context: IContext
    {
        List<PdbParseError> errors = new List<PdbParseError>();
        public void AddError(PdbParseError error) => errors.Add(error);
        public ImmutableArray<PdbParseError> Errors => errors.ToImmutableArray();
    }
    public class ThreadSafeContext: IContext
    {
        readonly ConcurrentBag<PdbParseError> errors = new ConcurrentBag<PdbParseError>();
        public void AddError(PdbParseError error) => errors.Add(error);
        public ImmutableArray<PdbParseError> Errors => errors.ToImmutableArray();
    }
    public async Task<PdbParseResult<Pdb>> ParseAsync(string projectDirectory, DebugFiles debugFiles, CancellationToken ct = default)
    {
        var reportTask = Task.Run(() => ParseReport(debugFiles.Report), ct);
        var labels = await Task.Run(() => ParseLabels(debugFiles.Labels, ct), ct);
        var report = await reportTask;

        var context = new ThreadSafeContext();
        var acmePdb = CreatePdb(projectDirectory, 
            Path.GetDirectoryName(debugFiles.Report) ?? throw new Exception("Couldn't create ACME paths"), 
            report.ParsedData, labels.ParsedData, context);
        var errors = context.Errors;
        var allErrors = errors.Union(report.Errors).Union(labels.Errors).ToImmutableArray();

        return PdbParseResultBuilder.Create(acmePdb, allErrors);
    }
    /// <summary>
    /// Maps report lines into files and nested lines and returns AcmePdb
    /// </summary>
    /// <param name="report"></param>
    /// <param name="labels"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    internal Pdb CreatePdb(string projectDirectory, string reportDirectory, ImmutableArray<ReportLine> report, 
        ImmutableDictionary<string, PdbLabel> labels, IContext context)
    {
        var linesBuilder = ImmutableArray.CreateBuilder<PdbLine>();
        var filesBuilder = ImmutableDictionary.CreateBuilder<PdbPath, PdbFile>();
        PdbFile? file = null;
        var lineToFileMapBuilder = PdbFile.CreateLineToFileMapBuilder();
        foreach (var reportLine in report)
        {
            switch (reportLine)
            {
                case ReportSource reportSource:
                    var path = new PdbPath(reportSource.RelativePath, IsRelative: true);
                    if (!filesBuilder.TryGetValue(path, out file))
                    {
                        string sourceFilePath = reportSource.RelativePath;
                        // source file could be relative to project
                        if (sourceFilePath.StartsWith('.'))
                        {
                            sourceFilePath = Path.Combine(projectDirectory, sourceFilePath);
                        }
                        string relativePath = Path.GetRelativePath(projectDirectory, sourceFilePath);
                        file = PdbFile.CreateFromRelativePath(relativePath);
                        filesBuilder.Add(file.Path, file);
                    }
                    break;
                case ReportCodeLine reportCodeLine:
                    if (file is not null)
                    {
                        ushort dataLength;
                        if (reportCodeLine.Data.Length > 0 && !(reportCodeLine.IsMoreData ?? false))
                        {
                            dataLength = (ushort)reportCodeLine.Data.Length;
                        }
                        else
                        {
                            dataLength = 0;
                        }
                        var line = new PdbLine(reportCodeLine.LineNumber, reportCodeLine.StartAddress, 
                            reportCodeLine.Data, dataLength, reportCodeLine.IsMoreData, reportCodeLine.Text);
                        lineToFileMapBuilder.Add(line, file);
                        linesBuilder.Add(line);
                    }
                    else
                    {
                        context.AddError(new PdbParseError(reportCodeLine.LineNumber, reportCodeLine.Text, "No assigned file"));
                    }
                    break;
            }
        }
        var files = filesBuilder.Values.ToImmutableArray();
        var rawLines = linesBuilder.ToArray();
        var lines = FixLinesDataLength(rawLines, lineToFileMapBuilder);
        var lineToFileMap = lineToFileMapBuilder.ToImmutableDictionary();
        PdbFile[] acmeFiles = new PdbFile[files.Length];
        if (acmeFiles.Length > 1)
        {
            Parallel.For(0, files.Length, (index, loopState) =>
            {
                acmeFiles[index] = CreateAcmeFile(files[index], lines, lineToFileMap);
            });
        }
        else
        {
            acmeFiles[0] = files[0] with { Lines = lines };
        }
        var finalLineToFileMap = (from f in acmeFiles
                                  from l in f.Lines
                                  select new { File = f, Line = l })
                                  .ToImmutableDictionary(p => p.Line, p => p.File, RhReferenceEqualityComparer<PdbLine>.Instance);
        var linesWithEmptyFile = lineToFileMap.Values.Where(f => f.Lines.IsEmpty);
        return new Pdb(
            acmeFiles.ToImmutableDictionary(f => f.Path, f => f), 
            labels,
            finalLineToFileMap,
            ImmutableDictionary<string, PdbVariable>.Empty,
            ImmutableDictionary<int, PdbType>.Empty,
            lines.Where(l => l.StartAddress.HasValue).ToImmutableArray());
    }
    /// <summary>
    /// Adds DataLength to those lines with more data than reported by ACME
    /// </summary>
    /// <param name="source"></param>
    /// <param name="linesToFileBuilder">Maps that should be updated</param>
    /// <returns>An immutable array of lines with set DataLength</returns>
    internal ImmutableArray<PdbLine> FixLinesDataLength(PdbLine[] source, 
        ImmutableDictionary<PdbLine, PdbFile>.Builder linesToFileBuilder)
    {
        if (source.Length == 0)
        {
            return ImmutableArray<PdbLine>.Empty;
        }
        PdbLine? incomplete = null;
        int? incompleteIndex = null;
        int i = 0;
        while (i < source.Length)
        {
            bool proceedWithNext = true;
            PdbLine line = source[i];
            if (incomplete is null)
            {
                if (line.HasMoreData ?? false)
                {
                    incomplete = line;
                    incompleteIndex = i;
                }
            }
            else
            {
                if (line.StartAddress.HasValue)
                {
                    var existingLine = incomplete;
                    incomplete = incomplete with { DataLength = (ushort)(line.StartAddress!.Value - incomplete.StartAddress!.Value) };
                    source[incompleteIndex!.Value] = incomplete;
                    UpdateLineToFileMap(linesToFileBuilder, existingLine, incomplete);
                    incomplete = null;
                    incompleteIndex = null;
                    proceedWithNext = !(line.HasMoreData ?? false);
                }
            }
            // when a line with StartAddress if found
            if (proceedWithNext)
            {
                i++;
            }
        }
        // fix last line, just set the length to whatever
        PdbLine lastLine = source[^1];
        if (lastLine.HasMoreData ?? false)
        {
            var existingLine = lastLine;
            lastLine = lastLine with { DataLength = (ushort)lastLine.Data!.Value.Length };
            source[^1] = lastLine;
            UpdateLineToFileMap(linesToFileBuilder, existingLine, lastLine);
        }
        return source.ToImmutableArray();
    }
    internal static void UpdateLineToFileMap(ImmutableDictionary<PdbLine, PdbFile>.Builder linesToFileBuilder, 
        PdbLine oldLine, PdbLine newLine)
    {
        // update mappings
        var file = linesToFileBuilder[oldLine];
        linesToFileBuilder.Remove(oldLine);
        linesToFileBuilder.Add(newLine, file);
    }
    internal PdbFile CreateAcmeFile(PdbFile source, ImmutableArray<PdbLine> lines, ImmutableDictionary<PdbLine, PdbFile> map)
    {
        var fileLines = lines
            .Where(l => PdbFileByPathEqualityComparer.Instance.Equals(map[l], source))
            .ToImmutableArray();
        return source with { Lines = fileLines };
    }
    internal string BeautifyRelativePath(string path)
    {
        if (path.StartsWith(@".\"))
        {
            return path.Substring(2);
        }
        return path;
    }
    internal PdbParseResult<ImmutableDictionary<string, PdbLabel>> ParseLabels(string path, CancellationToken ct)
    {
        using (var stream = File.OpenRead(path))
        {
            var labels = ParseLabels(stream);
            var dictionary = labels.ParsedData.ToImmutableDictionary(l => l.Name, l => l);
            return PdbParseResultBuilder.Create(dictionary, labels.Errors);
        }
    }
    internal PdbParseResult<ImmutableArray<PdbLabel>> ParseLabels(Stream stream)
    {
        var builder = ImmutableArray.CreateBuilder<PdbLabel>();
        Context context = new();
        using (var sr = new StreamReader(stream, Encoding.UTF8))
        {
            string? line;
            int lineNumber = 0;
            while ((line = sr.ReadLine()) is not null)
            {
                var label = ParseLabel(line, lineNumber, context);
                if (label is not null)
                {
                    builder.Add(label);
                }
            }
        }
        return PdbParseResultBuilder.Create(builder.ToImmutable(), context.Errors.ToImmutableArray());
    }
    internal PdbLabel? ParseLabel(ReadOnlySpan<char> text, int lineNumber, Context context)
    {
        if (text.IsWhiteSpace())
        {
            return null;
        }
        else
        {
            if (!ushort.TryParse(text[5..9], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort value))
            {
                context.Errors.Add(new PdbParseError(lineNumber, text.ToString(), "Failed parsing label address"));
                return null;
            }
            return new PdbLabel(value, text[11..].ToString());
        }
    }
    internal PdbParseResult<ImmutableArray<ReportLine>> ParseReport(string path)
    {
        const int ParallelThreshold = 100;
        var sourceLines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToImmutableArray();
        ReportLine?[] reportLines = new ReportLine[sourceLines.Length];
        IContext context;
        if (sourceLines.Length < ParallelThreshold)
        {
            context = new Context();
            ProcessSourceLines(reportLines, sourceLines, 0, sourceLines.Length, context);
        }
        else
        {
            context = new ThreadSafeContext();
            var partitioner = Partitioner.Create(0, sourceLines.Length);
            Parallel.ForEach(partitioner, (range, loopState) =>
            {
                ProcessSourceLines(reportLines, sourceLines, range.Item1, range.Item2, context);
            });
        }
        return PdbParseResultBuilder.Create(reportLines.Where(l => l is not null).Select(l => l!).ToImmutableArray(), context.Errors);
    }
    internal void ProcessSourceLines(ReportLine?[] reportLines, ImmutableArray<string> sourceLines, int fromInclusive, int toExclusive, IContext context)
    {
        for (int i = 0; i < sourceLines.Length; i++)
        {
            reportLines[i] = ParseCodeLine(sourceLines[i], i, context);
        }
    }
    /// <summary>
    /// Parses line of code and returns either <see cref="ReportSource"/> or <see cref="ReportCodeLine"/>
    /// </summary>
    /// <param name="line"></param>
    /// <param name="lineNumber"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    internal ReportLine? ParseCodeLine(string line, int lineNumber, IContext context)
    {
        const string SourcePrefix = "; ******** Source: ";
        if (line.StartsWith(SourcePrefix))
        {
            return new ReportSource(line.Substring(SourcePrefix.Length));
        }
        else
        {
            var span = line.AsSpan();
            ushort? startAddress;
            var fileLineNumberText = span[0..6];
            if (!int.TryParse(fileLineNumberText, out int fileLineNumber))
            {
                context.AddError(new PdbParseError(lineNumber, line, "Failed to parse line number"));
                return null;
            }
            var addressText = span[8..12];
            if (!addressText.IsWhiteSpace())
{
                if (!ushort.TryParse(addressText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort address))
                {
                    context.AddError(new PdbParseError(lineNumber, line, "Failed parsing address"));
                    return null;
                }
                else
                {
                    startAddress = address;
                }
            }
            else
            {
                startAddress = null;
            }
            var bytesBuilder = ImmutableArray.CreateBuilder<byte>(8);
            bool? isMoreData;
            if (startAddress.HasValue)
            {
                for (int i = 13; i < 29; i+=2)
                {
                    var byteText = span[i..(i + 2)];
                    if (!byteText.IsWhiteSpace())
                    {
                        if (!byte.TryParse(byteText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte byteValue))
                        {
                            context.Errors.Add(new PdbParseError(lineNumber, line, "Failed parsing bytes"));
                            return null;
                        }
                        bytesBuilder.Add(byteValue);
                    }
                    else
                    {
                        break;
                    }
                }
                var moreText = span[29..32];
                isMoreData = moreText.Equals("...", StringComparison.Ordinal);
            }
            else
            {
                isMoreData = null;
            }
            return new ReportCodeLine(fileLineNumber, startAddress, bytesBuilder.ToImmutable(), isMoreData, line.Substring(32));
        }
    }
}

public abstract record ReportLine;
public record ReportCodeLine(int LineNumber, ushort? StartAddress, ImmutableArray<byte> Data, bool? IsMoreData, string Text): ReportLine;
public record ReportSource(string RelativePath): ReportLine;
