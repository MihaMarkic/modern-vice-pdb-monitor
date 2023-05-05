using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge;

namespace Modern.Vice.PdbMonitor.Engine;

public static class IoCRegistrar
{
    public static void AddEngine(this IServiceCollection services)
    {
        services.AddScoped<MainViewModel>()
            .AddScoped<SettingsViewModel>()
            .AddScoped<DebuggerViewModel>()
            .AddScoped<ErrorMessagesViewModel>()
            .AddScoped<ProjectExplorerViewModel>()
            .AddScoped<ProjectViewModel>()
            .AddScoped<SourceFileViewerViewModel>()
            .AddSingleton<RegistersViewModel>()
            .AddSingleton<VariablesViewModel>()
            .AddSingleton<RegistersMapping>()
            .AddSingleton<ExecutionStatusViewModel>()
            .AddSingleton<BreakpointsViewModel>()
            .AddSingleton<TraceOutputViewModel>()
            .AddTransient<SourceFileViewModel>()
            .AddSingleton<Globals>()
            .AddSingleton<ISettingsManager, SettingsManager>()
            .AddSingleton<IDispatcher, Dispatcher>()
            .AddSingleton<IProjectPrgFileWatcher, ProjectPrgFileWatcher>()
            .AddSingleton<IPdbManager, PdbManager>()
            .AddTransient(sp => sp.CreateScope())
            .AddTransient<IProjectFactory, ProjectFactory>()
            .AddSingleton<AssemblyDebugStepper>()
            .AddSingleton<HighLevelDebugStepper>()
            .AddViceBridge();
    }
}
