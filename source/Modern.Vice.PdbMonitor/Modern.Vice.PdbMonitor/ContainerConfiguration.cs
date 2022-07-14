using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine;
using NLog.Extensions.Logging;

namespace Modern.Vice.PdbMonitor;

public static class ContainerConfiguration
{
    public static IServiceCollection Configure(this IServiceCollection services)
    {
        var config = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             //.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
             .Build();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddNLog(config);
        });
        services.AddEngine();
        services.AddCore();
        services.AddAcme();
        return services;
    }
}
