using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine
{
    public static class IoCRegistrar
    {
        public static void AddEngine(this IServiceCollection services)
        {
            services.AddScoped<MainViewModel>();
            services.AddScoped<SettingsViewModel>();
            services.AddSingleton<Globals>();
            services.AddSingleton<IAcmePdbParser, AcmePdbParser>();
            services.AddSingleton<ISettingsManager, SettingsManager>();
            services.AddSingleton<IDispatcher, Dispatcher>();
        }
    }
}
