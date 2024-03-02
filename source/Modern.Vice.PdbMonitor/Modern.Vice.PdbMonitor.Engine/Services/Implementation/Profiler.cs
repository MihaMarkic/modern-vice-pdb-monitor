using Microsoft.Extensions.Logging;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;
public sealed class Profiler : IProfiler
{
    readonly ILogger<Profiler> logger;
    readonly IViceBridge viceBridge;
    public bool IsActive { get; private set; }
    public event EventHandler? IsActiveChanged;
    public Profiler(ILogger<Profiler> logger, IViceBridge viceBridge)
    {
        this.logger = logger;
        this.viceBridge = viceBridge;
    }

    public void Start()
    {
        if (!IsActive)
        {
            IsActive = true;
            OnIsActiveChanged();
            viceBridge.ViceResponse += ViceBridge_ViceResponse;
            viceBridge.ConnectedChanged += ViceBridge_ConnectedChanged;
        }
    }

    private void ViceBridge_ConnectedChanged(object? sender, ConnectedChangedEventArgs e)
    {
    }

    private void ViceBridge_ViceResponse(object? sender, ViceResponseEventArgs e)
    {
    }

    public Task StopAsync()
    {
        if (IsActive)
        {
            viceBridge.ViceResponse -= ViceBridge_ViceResponse;
            viceBridge.ConnectedChanged -= ViceBridge_ConnectedChanged;
            IsActive = false;
            OnIsActiveChanged();
        }
        return Task.CompletedTask;
    }

    void OnIsActiveChanged() => IsActiveChanged?.Invoke(this, EventArgs.Empty);

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
