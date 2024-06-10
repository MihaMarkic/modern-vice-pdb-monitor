using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Core.Services.Abstract;

namespace Assembler.KickAssembler;
public class KickAssemblerServices : ICompilerServices
{
    Task<(Pdb? Pdb, string? ErrorMessage)> ICompilerServices.ParseDebugSymbolsAsync(string projectDirectory, string prgPath, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
