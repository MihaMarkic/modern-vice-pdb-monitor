using Modern.Vice.PdbMonitor.Engine.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract
{
    public interface IAcmePdbParser
    {
        Task<AcmePdbParseResult> ParseAsync(Stream stream, CancellationToken ct = default);
        Task<AcmePdbParseResult> ParseAsync(string path, CancellationToken ct = default);
    }
}
