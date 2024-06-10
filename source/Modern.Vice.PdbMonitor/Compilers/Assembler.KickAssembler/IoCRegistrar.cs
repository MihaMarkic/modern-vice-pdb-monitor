using Microsoft.Extensions.DependencyInjection;

namespace Assembler.KickAssembler;

public static class IoCRegistrar
{
    public static void AddKickAssembler(this IServiceCollection services)
    {
        services.AddTransient<KickAssemblerServices>();
        services.AddTransient<KickAssembler>();
        //services.AddSingleton<Oscar64DbjParser>();
        //services.AddSingleton<AsmParser>();
    }
}
