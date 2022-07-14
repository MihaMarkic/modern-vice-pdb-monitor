using System;
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
            _ => throw new Exception($"Unknown compiler type {compilerType}"),
        };
    }
    public ICompiler GetCompiler(CompilerType compilerType)
    {
        return compilerType switch
        {
            CompilerType.Acme => serviceProvider.GetRequiredService<AcmeCompiler>(),
            _ => throw new Exception($"Unknown compiler type {compilerType}"),
        };
    }
    public IPdbManager GetPdbManager(CompilerType compilerType)
    {
        return compilerType switch
        {
            CompilerType.Acme => serviceProvider.GetRequiredService<IPdbManager>(),
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
                "ACME Compiled CBM file .prg",
                "prg"),
            _ => throw new Exception($"Unknown compiler type {compilerType}"),
        };
    }
}
