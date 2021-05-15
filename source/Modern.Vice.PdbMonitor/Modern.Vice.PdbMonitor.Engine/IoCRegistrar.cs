using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge;

namespace Modern.Vice.PdbMonitor.Engine
{
    public static class IoCRegistrar
    {
        public static void AddEngine(this IServiceCollection services)
        {
            services.AddScoped<MainViewModel>();
            services.AddScoped<SettingsViewModel>();
            services.AddScoped<DebuggerViewModel>();
            services.AddScoped<ErrorMessagesViewModel>();
            services.AddScoped<ProjectExplorerViewModel>();
            services.AddScoped<ProjectViewModel>();
            services.AddScoped<SourceFileViewerViewModel>();
            services.AddSingleton<RegistersViewModel>();
            services.AddSingleton<RegistersMapping>();
            services.AddTransient<SourceFileViewModel>();
            services.AddSingleton<Globals>();
            services.AddSingleton<IAcmePdbParser, AcmePdbParser>();
            services.AddSingleton<ISettingsManager, SettingsManager>();
            services.AddSingleton<IDispatcher, Dispatcher>();
            services.AddSingleton<IProjectPrgFileWatcher, ProjectPrgFileWatcher>();
            services.AddViceBridge();
            services.AddTransient(sp => sp.CreateScope());
        }
    }
}
