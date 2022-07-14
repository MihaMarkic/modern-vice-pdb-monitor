using System.Threading;
using System.Threading.Tasks;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Core.Common.Compiler;

namespace Modern.Vice.PdbMonitor.Compilers.Acme.Services.Abstract;

public interface IAcmePdbParser
{
    Task<PdbParseResult<Pdb>> ParseAsync(string projectDirectory, DebugFiles debugFiles, CancellationToken ct = default);
}
