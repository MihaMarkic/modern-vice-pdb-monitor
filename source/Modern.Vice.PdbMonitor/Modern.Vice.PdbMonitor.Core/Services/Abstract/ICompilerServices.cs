using System.Threading;
using System.Threading.Tasks;
using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Core.Services.Abstract;

public interface ICompilerServices
{
    Task<(Pdb? Pdb, string? ErrorMessage)> ParseDebugSymbolsAsync(string projectDirectory, string prgPath, CancellationToken ct);
}
