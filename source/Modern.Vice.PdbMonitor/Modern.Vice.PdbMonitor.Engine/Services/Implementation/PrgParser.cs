using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;
/// <summary>
/// Parses prg file, see https://michaelcmartin.github.io/Ophis/book/x72.html
/// </summary>
public class PrgParser : IPrgParser
{
    readonly ILogger<PrgParser> logger;
    readonly IFileService fileService;
    public PrgParser(ILogger<PrgParser> logger, IFileService fileService)
    {
        this.logger = logger;
        this.fileService = fileService;
    }
    /// <inheritdoc/>
    public ushort GetEntryAddress(string path)
    {
        Span<byte> buffer = stackalloc byte[2];
        using (var stream = fileService.OpenFileStream(path))
        {
            stream.ReadExactly(buffer);
        }
        return BitConverter.ToUInt16(buffer);
    }

    /// <inheritdoc/>
    public ushort GetStartAddress(string path)
    {
        Span<byte> buffer = stackalloc byte[12];
        using (var stream = fileService.OpenFileStream(path))
        {
            stream.ReadExactly(buffer);
        }
        if (buffer[6] != 0x9E)
        {
            throw new Exception("Expected 0x209E at position 4");
        }
        if (buffer[^1] != 0x00)
        {
            throw new Exception("Expected 0x00 at last position");
        }
        ushort address = (ushort)(GetDigit(buffer[7]) * 1000 + GetDigit(buffer[8]) * 100
            + GetDigit(buffer[9]) * 10 + GetDigit(buffer[10]));
        return address;
    }

    byte GetDigit(byte digit)
    {
        if (digit < 0x30 || digit > 0x39)
        {
            throw new ArgumentOutOfRangeException($"Value {digit} should be between 0x30 and 0x3A", nameof(digit));
        }
        return (byte)(digit - 0x30);
    }
}
