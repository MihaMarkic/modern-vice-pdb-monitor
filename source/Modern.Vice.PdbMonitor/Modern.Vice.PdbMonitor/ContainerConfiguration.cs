using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine;
using NLog.Extensions.Logging;
using Compiler.Oscar64;

namespace Modern.Vice.PdbMonitor;

public static class ContainerConfiguration
{
    public static IServiceCollection Configure(this IServiceCollection services)
    {
        var config = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
             .Build();

        bool messagesHistory = config.GetValue<bool>("Application:MessagesHistory", false);

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddNLog(config);
        });
        services.AddEngine(messagesHistory);
        services.AddCore();
        services.AddAcme();
        services.AddOscar64();
        return services;
    }
}
