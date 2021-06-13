using System.Threading;
using System.Threading.Tasks;
using Modern.Vice.PdbMonitor.Engine.Models;

namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract
{
    public interface IAcmePdbParser
    {
        Task<AcmePdbParseResult<AcmePdb>> ParseAsync(string projectDirectory, DebugFiles debugFiles, CancellationToken ct = default);
    }
}
