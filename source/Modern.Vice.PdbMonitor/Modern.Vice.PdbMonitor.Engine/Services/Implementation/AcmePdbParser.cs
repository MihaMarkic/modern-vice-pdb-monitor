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
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation
{
    public class AcmePdbParser : IAcmePdbParser
    {
        public interface IContext
        {
            abstract void AddError(AcmePdbParseError error);
            abstract ImmutableArray<AcmePdbParseError> Errors { get; }
        }
        public class Context: IContext
        {
            List<AcmePdbParseError> errors = new List<AcmePdbParseError>();
            public void AddError(AcmePdbParseError error) => errors.Add(error);
            public ImmutableArray<AcmePdbParseError> Errors => errors.ToImmutableArray();
        }
        public class ThreadSafeContext: IContext
        {
            readonly ConcurrentBag<AcmePdbParseError> errors = new ConcurrentBag<AcmePdbParseError>();
            public void AddError(AcmePdbParseError error) => errors.Add(error);
            public ImmutableArray<AcmePdbParseError> Errors => errors.ToImmutableArray();
        }
        public async Task<AcmePdbParseResult<AcmePdb>> ParseAsync(string projectDirectory, DebugFiles debugFiles, CancellationToken ct = default)
        {
            var reportTask = Task.Run(() => ParseReport(debugFiles.Report), ct);
            var labels = await Task.Run(() => ParseLabels(debugFiles.Labels, ct), ct);
            var report = await reportTask;

            var context = new ThreadSafeContext();
            var acmePdb = CreatePdb(projectDirectory, debugFiles.Report, report.ParsedData, labels.ParsedData, context);
            var errors = context.Errors;
            var allErrors = errors.Union(report.Errors).Union(labels.Errors).ToImmutableArray();

            return AcmePdbParseResultBuilder.Create(acmePdb, allErrors);
        }
        /// <summary>
        /// Maps report lines into files and nested lines and returns AcmePdb
        /// </summary>
        /// <param name="report"></param>
        /// <param name="labels"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal AcmePdb CreatePdb(string projectDirectory, string reportDirectory, ImmutableArray<ReportLine> report, ImmutableDictionary<string, AcmeLabel> labels, IContext context)
        {
            var linesBuilder = ImmutableArray.CreateBuilder<AcmeLine>();
            var filesBuilder = ImmutableDictionary.CreateBuilder<string, AcmeFile>();
            AcmeFile? file = null;
            foreach (var reportLine in report)
            {
                switch (reportLine)
                {
                    case ReportSource reportSource:
                        if (!filesBuilder.TryGetValue(reportSource.RelativePath, out file))
                        {
                            string reportFilePath = reportSource.RelativePath;
                            // source file could be relative to report
                            if (reportFilePath.StartsWith('.'))
                            {
                                reportFilePath = Path.Combine(reportDirectory, reportFilePath);
                            }
                            string relativePath = Path.GetRelativePath(projectDirectory, reportSource.RelativePath);
                            file = new AcmeFile(relativePath);
                            filesBuilder.Add(file.RelativePath, file);
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
                            var line = new AcmeLine(file.RelativePath, reportCodeLine.LineNumber, reportCodeLine.StartAddress, 
                                reportCodeLine.Data, dataLength, reportCodeLine.IsMoreData, reportCodeLine.Text);
                            linesBuilder.Add(line);
                        }
                        else
                        {
                            context.AddError(new AcmePdbParseError(reportCodeLine.LineNumber, reportCodeLine.Text, "No assigned file"));
                        }
                        break;
                }
            }
            var files = filesBuilder.Values.ToImmutableArray();
            var rawLines = linesBuilder.ToArray();
            var lines = FixLinesDataLength(rawLines);
            AcmeFile[] acmeFiles = new AcmeFile[files.Length];
            if (acmeFiles.Length > 1)
            {
                Parallel.For(0, files.Length, (index, loopState) =>
                {
                    acmeFiles[index] = CreateAcmeFile(files[index], lines);
                });
            }
            else
            {
                acmeFiles[0] = files[0] with { Lines = lines };
            }
            return new AcmePdb(lines, acmeFiles.ToImmutableDictionary(f => f.RelativePath, f => f), labels,
                lines.Where(l => l.StartAddress.HasValue).ToImmutableArray());
        }
        /// <summary>
        /// Adds DataLength to those lines with more data than reported by ACME
        /// </summary>
        /// <param name="source"></param>
        /// <returns>An immutable array of lines with set DataLength</returns>
        internal ImmutableArray<AcmeLine> FixLinesDataLength(AcmeLine[] source)
        {
            if (source.Length == 0)
            {
                return ImmutableArray<AcmeLine>.Empty;
            }
            AcmeLine? incomplete = null;
            int? incompleteIndex = null;
            int i = 0;
            while (i < source.Length)
            {
                bool proceedWithNext = true;
                AcmeLine line = source[i];
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
                        incomplete = incomplete with { DataLength = (ushort)(line.StartAddress!.Value - incomplete.StartAddress!.Value) };
                        source[incompleteIndex!.Value] = incomplete;
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
            AcmeLine lastLine = source[^1];
            if (lastLine.HasMoreData ?? false)
            {
                lastLine = lastLine with { DataLength = (ushort)lastLine.Data!.Value.Length };
                source[^1] = lastLine;
            }
            return source.ToImmutableArray();
        }
        internal AcmeFile CreateAcmeFile(AcmeFile source, ImmutableArray<AcmeLine> lines)
        {
            var fileLines = lines.Where(l => l.FileRelativePath == source.RelativePath).ToImmutableArray();
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
        internal AcmePdbParseResult<ImmutableDictionary<string, AcmeLabel>> ParseLabels(string path, CancellationToken ct)
        {
            using (var stream = File.OpenRead(path))
            {
                var labels = ParseLabels(stream);
                var dictionary = labels.ParsedData.ToImmutableDictionary(l => l.Name, l => l);
                return AcmePdbParseResultBuilder.Create(dictionary, labels.Errors);
            }
        }
        internal AcmePdbParseResult<ImmutableArray<AcmeLabel>> ParseLabels(Stream stream)
        {
            var builder = ImmutableArray.CreateBuilder<AcmeLabel>();
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
            return AcmePdbParseResultBuilder.Create(builder.ToImmutable(), context.Errors.ToImmutableArray());
        }
        internal AcmeLabel? ParseLabel(ReadOnlySpan<char> text, int lineNumber, Context context)
        {
            if (text.IsWhiteSpace())
            {
                return null;
            }
            else
            {
                if (!ushort.TryParse(text[5..9], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort value))
                {
                    context.Errors.Add(new AcmePdbParseError(lineNumber, text.ToString(), "Failed parsing label address"));
                    return null;
                }
                return new AcmeLabel(value, text[11..].ToString());
            }
        }
        internal AcmePdbParseResult<ImmutableArray<ReportLine>> ParseReport(string path)
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
            return AcmePdbParseResultBuilder.Create(reportLines.Where(l => l is not null).Select(l => l!).ToImmutableArray(), context.Errors);
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
                    context.AddError(new AcmePdbParseError(lineNumber, line, "Failed to parse line number"));
                    return null;
                }
                var addressText = span[8..12];
                if (!addressText.IsWhiteSpace())
{
                    if (!ushort.TryParse(addressText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort address))
                    {
                        context.AddError(new AcmePdbParseError(lineNumber, line, "Failed parsing address"));
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
                                context.Errors.Add(new AcmePdbParseError(lineNumber, line, "Failed parsing bytes"));
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
}
