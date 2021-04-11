using Microsoft.Extensions.Hosting;

namespace Modern.Vice.PdbMonitor.Core
{
    public static class IoC
    {
        public static IHost Host { get; private set; } = default!;
        /// <summary>
        /// Has to be called before IoC is used, usually at very program start.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public static void Init(IHost host)
        {
            Host = host;
        }
    }
}
