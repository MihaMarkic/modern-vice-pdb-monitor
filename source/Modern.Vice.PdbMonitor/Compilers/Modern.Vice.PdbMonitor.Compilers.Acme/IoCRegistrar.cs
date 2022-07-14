using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Compilers.Acme;
using Modern.Vice.PdbMonitor.Compilers.Acme.Services.Abstract;
using Modern.Vice.PdbMonitor.Compilers.Acme.Services.Implementation;

namespace Modern.Vice.PdbMonitor.Engine;

public static class IoCRegistrar
{
    public static void AddAcme(this IServiceCollection services)
    {
        services.AddTransient<IAcmePdbParser, AcmePdbParser>();
        services.AddTransient<AcmeCompilerServices>();
        services.AddTransient<AcmeCompiler>();
    }
}
