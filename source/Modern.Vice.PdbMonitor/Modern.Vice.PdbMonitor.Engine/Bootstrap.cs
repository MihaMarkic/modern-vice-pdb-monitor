using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Engine;

public static class Bootstrap
{
    static Globals globals = default!;
    public static void Init(IServiceScope scope)
    {
        globals = scope.ServiceProvider.GetService<Globals>()!;
        globals.Load();
    }
    public static void Close()
    {
        globals.Save();
    }
}
