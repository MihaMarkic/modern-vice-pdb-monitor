using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation
{
    public class AcmePdbParser : IAcmePdbParser
    {
        public class Context
        {
            public List<AcmePdbParseError> Errors { get; set; } = new List<AcmePdbParseError>();
        }
        public async Task<AcmePdbParseResult> ParseAsync(string path, CancellationToken ct = default)
        {
            using (var stream = File.OpenRead(path))
            {
                return await ParseAsync(stream, ct);
            }
        }
        public async Task<AcmePdbParseResult> ParseAsync(Stream stream, CancellationToken ct = default)
        {
            var result = AcmePdb.Empty;
            Context context = new();
            int lineNumber = 0;
            using (var sr = new StreamReader(stream, Encoding.ASCII))
            {
                string? line;
                while (context.Errors.Count == 0 && (line = await sr.ReadLineAsync()) is not null)
                {
                    var caption = ParseCaption(lineNumber, line, context);
                    if (caption is null)
                    {
                        break;
                    }
                    switch (caption.Value.Caption)
                    {
                        case "INCLUDES":
                            result = result with
                            {
                                Includes = await GetItemsAsync<AcmePdbInclude>(lineNumber + 1, caption.Value.Count, sr, context, (_, line, _) => new(line), ct)
                            };
                            lineNumber += result.Includes.Length;
                            break;
                        case "FILES":
                            result = result with
                            {
                                Files = await GetItemsAsync(lineNumber + 1, caption.Value.Count, sr, context, GetFile, ct)
                            };
                            lineNumber += result.Files.Length;
                            break;
                        case "ADDRS":
                            result = result with
                            {
                                Addresses = await GetItemsAsync(lineNumber + 1, caption.Value.Count, sr, context, GetAddress, ct)
                            };
                            lineNumber += result.Addresses.Length;
                            break;
                        case "LABELS":
                            result = result with
                            {
                                Labels = await GetItemsAsync(lineNumber + 1, caption.Value.Count, sr, context, GetLabel, ct)
                            };
                            lineNumber += result.Labels.Length;
                            break;
                        default:
                            context.Errors.Add(new AcmePdbParseError(lineNumber, line, "Expected header line"));
                            break;
                    }
                }
                return new AcmePdbParseResult(result, context.Errors.ToImmutableArray());
            }
        }
        internal async Task<ImmutableArray<T>> GetItemsAsync<T>(int startLineNumber, int count, StreamReader sr, Context context,
            Func<int, string, Context, T?> getItem, CancellationToken ct = default)
        {
            var result = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                string? line = await sr.ReadLineAsync();
                if (line is null)
                {
                    context.Errors.Add(new AcmePdbParseError(startLineNumber + i, string.Empty, "Failed reading line for includes"));
                    break;
                }
                else
                {
                    var item = getItem(startLineNumber + i, line, context);
                    if (item is not null)
                    {
                        result.Add(item);
                    }
                    // an error occurred
                    else
                    {
                        break;
                    }
                }
            }
            return result.ToImmutableArray();
        }
        internal AcmePdbFile? GetFile(int lineNumber, string line, Context context)
        {
            string[] parts = line.Split(':');
            if (parts.Length != 2)
            {
                context.Errors.Add(new AcmePdbParseError(lineNumber, line.ToString(), "File item should have two parts separated by ':'"));
                return null;
            }
            if (!int.TryParse(parts[0], out var fileIndex))
            {
                context.Errors.Add(new AcmePdbParseError(lineNumber, line.ToString(), $"File item index '{parts[0]}' is invalid number"));
                return null;
            }
            return new(fileIndex, parts[1]);
        }
        internal AcmePdbAddress? GetAddress(int lineNumber, string line, Context context)
        {
            string[] parts = line.Split(':');
            if (parts.Length != 4)
            {
                context.Errors.Add(new AcmePdbParseError(lineNumber, line.ToString(), "Address item should have four parts separated by ':'"));
                return null;
            }
            if (!parts[0].StartsWith('$'))
            {
                context.Errors.Add(new AcmePdbParseError(lineNumber, line.ToString(), "Address item address value should start with '$'"));
                return null;
            }
            var addressText = parts[0].AsSpan()[1..];
            if (!int.TryParse(addressText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var address))
            {
                context.Errors.Add(new AcmePdbParseError(lineNumber, line.ToString(), $"Address item address '{addressText.ToString()}' is invalid hex number"));
                return null;
            }
            if (!ParseInt(lineNumber, parts[1], "Address", context, out var zone))
            {
                return null;
            }
            if (!ParseInt(lineNumber, parts[2], "Address", context, out var fileIndex))
            {
                return null;
            }
            if (!ParseInt(lineNumber, parts[3], "Address", context, out var sourceLine))
            {
                return null;
            }
            return new(address, zone, fileIndex, sourceLine);
        }
        internal AcmePdbLabel? GetLabel(int lineNumber, string line, Context context)
        {
            const string ItemType = "Label";
            string[] parts = line.Split(':');
            if (parts.Length != 5)
            {
                context.Errors.Add(new AcmePdbParseError(lineNumber, line.ToString(), $"{ItemType} item should have four parts separated by ':'"));
                return null;
            }
            if (!parts[0].StartsWith('$'))
            {
                context.Errors.Add(new AcmePdbParseError(lineNumber, line.ToString(), $"{ItemType} item address value should start with '$'"));
                return null;
            }
            var addressText = parts[0].AsSpan()[1..];
            if (!int.TryParse(addressText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var address))
            {
                context.Errors.Add(new AcmePdbParseError(lineNumber, line.ToString(), $"{ItemType} item address '{addressText.ToString()}' is invalid hex number"));
                return null;
            }
            if (!ParseInt(lineNumber, parts[1], ItemType, context, out var zone))
            {
                return null;
            }
            return new(address, zone, parts[2], parts[3] == "1", parts[4] == "1");
        }
        internal bool ParseInt(int lineNumber, string text, string itemType, Context context, out int value)
        {
            if (!int.TryParse(text, out value))
            {
                context.Errors.Add(new AcmePdbParseError(lineNumber, text, $"{itemType} item index '{text}' is invalid number"));
                return false;
            }
            return true;
        }
        internal (string Caption, int Count)? ParseCaption(int lineNumber, string line, Context context)
        {
            int separator = line.IndexOf(':');
            if (separator < 0)
            {
                context.Errors.Add(new AcmePdbParseError(lineNumber, line.ToString(), "Caption does not contain a separator ':'"));
                return null;
            }
            if (line.AsSpan().Slice(separator + 1).IndexOf(':') >= 0)
            {
                context.Errors.Add(new AcmePdbParseError(lineNumber, line.ToString(), "Caption contains more than one separator ':'"));
                return null;
            }
            if (!int.TryParse(line.AsSpan()[(separator + 1)..^0], out int count))
            {
                context.Errors.Add(new AcmePdbParseError(lineNumber, line.ToString(), "Caption does not contain number of items"));
                return null;
            }
            return (line.Substring(0, separator), count);
        }
    }
}
