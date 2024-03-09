using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public class ProfilerViewModel : NotifiableObject
{
    readonly ILogger<ProfilerViewModel> logger;
    readonly IProfiler profiler;
    public bool IsActive { get; private set; }
    public bool IsStarting {  get; private set; }
    public bool IsStopping { get; private set; }
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

    public async Task StartAsync()
    {
        IsStarting = true;
        try
        {
            await profiler.StartAsync(CancellationToken.None);
            Debug.WriteLine(profiler.IsActive ? "Profiler started": "Profiler cancelled");
        }
        finally
        {
            IsStarting = false;
        }
    }

    public async Task StopAsync()
    {
        IsStopping = true;
        try
        {
            await profiler.StopAsync();
        }
        finally
        {
            IsStopping = false;
        }
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
