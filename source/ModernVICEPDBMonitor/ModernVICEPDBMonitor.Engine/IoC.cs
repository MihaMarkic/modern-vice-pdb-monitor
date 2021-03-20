using Microsoft.Extensions.DependencyInjection;
using ModernVicePdbMonitor.Engine.Services.Abstract;
using ModernVicePdbMonitor.Engine.Services.Implementation;

namespace ModernVicePdbMonitor.Engine
{
    public static class IoC
    {
        public static void AddEngineServices(this IServiceCollection services)
        {
            services.AddSingleton<IViceBridge, ViceBridge>();
        }
    }
}
