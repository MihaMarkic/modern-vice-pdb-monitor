using Compiler.Oscar64.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Compiler.Oscar64;

public static class IoCRegistrar
{
    public static void AddOscar64(this IServiceCollection services)
    {
        services.AddTransient<Oscar64CompilerServices>();
        services.AddTransient<Oscar64Compiler>();
        services.AddSingleton<Oscar64DbjParser>();
        services.AddSingleton<AsmParser>();
    }
}
