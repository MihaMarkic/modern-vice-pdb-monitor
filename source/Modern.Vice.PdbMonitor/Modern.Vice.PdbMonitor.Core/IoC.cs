using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Core
{
    public static class IoC
    {
        public static IHost Host { get; private set; } = default!;
        /// <summary>
        /// Has to be called before IoC is used, usually at very program start.
        /// </summary>
        /// <param name="host"></param>
        public static void Init(IHost host)
        {
            Host = host;
        }

        public static void AddCore(this IServiceCollection services)
        {
            services.AddSingleton<EnumDisplayTextMapper>();
        }
    }
}
