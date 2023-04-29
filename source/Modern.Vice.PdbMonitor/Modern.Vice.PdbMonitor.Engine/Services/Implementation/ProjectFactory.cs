using System;
using Compiler.Oscar64;
using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Compilers.Acme;
using Modern.Vice.PdbMonitor.Core.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.Common;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;

/// <summary>
/// Factory for creating compiler specific instances.
/// </summary>
public class ProjectFactory : IProjectFactory
{
    readonly IServiceProvider serviceProvider;
    public ProjectFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }
    public ICompilerServices GetParser(CompilerType compilerType)
    {
        return compilerType switch
        {
            CompilerType.Acme => serviceProvider.GetRequiredService<AcmeCompilerServices>(),
            CompilerType.Oscar64 => serviceProvider.GetRequiredService<Oscar64CompilerServices>(),
            _ => throw new Exception($"Unknown compiler type {compilerType}"),
        };
    }
    public ICompiler GetCompiler(CompilerType compilerType)
    {
        return compilerType switch
        {
            CompilerType.Acme => serviceProvider.GetRequiredService<AcmeCompiler>(),
            CompilerType.Oscar64 => serviceProvider.GetRequiredService<Oscar64Compiler>(),
            _ => throw new Exception($"Unknown compiler type {compilerType}"),
        };
    }
    public IPdbManager GetPdbManager(CompilerType compilerType)
    {
        return compilerType switch
        {
            CompilerType.Acme => serviceProvider.GetRequiredService<IPdbManager>(),
            CompilerType.Oscar64 => serviceProvider.GetRequiredService<IPdbManager>(),
            _ => throw new Exception($"Unknown compiler type {compilerType}"),
        };
    }
    public DebugFileOpenDialogModel GetDebugFileOpenDialogModel(string? initialDirectory, CompilerType compilerType)
    {
        return compilerType switch
        {
            CompilerType.Acme => new DebugFileOpenDialogModel(
                initialDirectory,
                "Open output file",
                "ACME Compiled CBM files",
                "*.prg"),
            CompilerType.Oscar64 => new DebugFileOpenDialogModel(
                initialDirectory,
                "Open output file",
                "Oscar64 Compiled files",
                "*.prg"),
            _ => throw new Exception($"Unknown compiler type {compilerType}"),
        };
    }
}
