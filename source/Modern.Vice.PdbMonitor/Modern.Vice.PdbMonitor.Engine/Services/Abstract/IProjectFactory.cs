using Modern.Vice.PdbMonitor.Core.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.Common;

namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract;

public interface IProjectFactory
{
    ICompiler GetCompiler(CompilerType compilerType);
    ICompilerServices GetParser(CompilerType compilerType);
    IPdbManager GetPdbManager(CompilerType compilerType);
    DebugFileOpenDialogModel GetDebugFileOpenDialogModel(string? initialDirectory, CompilerType compilerType);
}
