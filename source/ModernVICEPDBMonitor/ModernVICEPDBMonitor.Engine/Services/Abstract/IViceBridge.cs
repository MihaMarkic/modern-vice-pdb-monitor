using System;
using System.Net;
using System.Threading.Tasks;

namespace ModernVicePdbMonitor.Engine.Services.Abstract
{
    public interface IViceBridge: IAsyncDisposable, IDisposable
    {
        Task? RunnerTask { get; }
        void Start(IPAddress address, int port = 6510);
        bool IsRunning { get; }
    }
}
