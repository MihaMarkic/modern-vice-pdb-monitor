using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModernVicePdbMonitor.Engine.Services.Abstract;
using Spectre.Console;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ModernVICEPDBMonitor.Playground
{
    public class Application
    {
        readonly ILogger logger;
        readonly IViceBridge bridge;
        public Application(ILogger<Application> logger, IViceBridge bridge)
        {
            this.logger = logger;
            this.bridge = bridge;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            try
            {
                bridge.Start(IPAddress.Loopback);
                AnsiConsole.WriteLine("Press ENTER to end");
                Console.ReadLine();
                await bridge.DisposeAsync();
                AnsiConsole.WriteLine("Main loop was cancelled");
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.WriteLine("Main loop was cancelled");
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                logger.LogError(ex, "Main loop failure");
            }
            Console.WriteLine("App stopped");
        }
    }
}
