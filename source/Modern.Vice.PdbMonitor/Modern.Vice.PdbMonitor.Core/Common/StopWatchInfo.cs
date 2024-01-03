using System;
using System.Diagnostics;

namespace Modern.Vice.PdbMonitor.Core.Common;
public class StopWatchInfo : IDisposable
{
    Stopwatch stopwatch = Stopwatch.StartNew();
    readonly string info;
    public StopWatchInfo(string info)
    {
        this.info = info;
        Debug.WriteLine($"Entering {info}");
    }
    void IDisposable.Dispose()
    {
        Debug.WriteLine($"{info}: {stopwatch.ElapsedMilliseconds:#,##0}ms");
    }
}
