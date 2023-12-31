using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine;

public static class IoCRegistrar
{
    public static void AddEngine(this IServiceCollection services, bool messagesHistoryEnabled)
    {
        services.AddScoped<MainViewModel>()
            .AddScoped<SettingsViewModel>()
            .AddScoped<DebuggerViewModel>()
            .AddScoped<ErrorMessagesViewModel>()
            .AddScoped<ProjectExplorerViewModel>()
            .AddScoped<ProjectViewModel>()
            .AddScoped<SourceFileViewerViewModel>()
            .AddScoped<MessagesHistoryViewModel>()
            .AddSingleton<RegistersViewModel>()
            .AddSingleton<VariablesViewModel>()
            .AddSingleton<WatchedVariablesViewModel>()
            .AddSingleton<RegistersMapping>()
            .AddSingleton<ExecutionStatusViewModel>()
            .AddSingleton<BreakpointsViewModel>()
            .AddSingleton<TraceOutputViewModel>()
            .AddTransient<SourceFileViewModel>()
            .AddSingleton<EmulatorMemoryViewModel>()
            .AddSingleton<Globals>()
            .AddSingleton<ISettingsManager, SettingsManager>()
            .AddSingleton<IDispatcher>(
            // uses dispatching from within same thread to all subscriptions by default as most subscribers are running on UI thread
            new Dispatcher(new DispatchContext(DispatchThreading.SameThread)))
            .AddSingleton<IProjectPrgFileWatcher, ProjectPrgFileWatcher>()
            .AddSingleton<IPdbManager, PdbManager>()
            .AddTransient(sp => sp.CreateScope())
            .AddTransient<IProjectFactory, ProjectFactory>()
            .AddSingleton<AssemblyDebugStepper>()
            .AddSingleton<HighLevelDebugStepper>()
            .AddViceBridge();
        if (messagesHistoryEnabled)
        {
            services.AddSingleton<IMessagesHistory, MessagesHistory>();
        }
    }
}
