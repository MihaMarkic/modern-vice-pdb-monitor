using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public class ProfilerViewModel : NotifiableObject
{
    readonly ILogger<ProfilerViewModel> logger;
    readonly IProfiler profiler;
    public bool IsActive { get; private set; }
    public ProfilerViewModel(ILogger<ProfilerViewModel> logger, IProfiler profiler)
    {
        this.logger = logger;
        this.profiler = profiler;
        profiler.IsActiveChanged += Profiler_IsActiveChanged;
    }

    private void Profiler_IsActiveChanged(object? sender, EventArgs e)
    {
        IsActive = profiler.IsActive;
    }

    public void Start()
    {
        profiler.Start();
    }

    public async Task Stop()
    {
        await profiler.StopAsync();
    }

    protected override void Dispose(bool disposing)
    { 
        if (disposing)
        {
            profiler.IsActiveChanged -= Profiler_IsActiveChanged;
        }
        base.Dispose(disposing);
    }
}
