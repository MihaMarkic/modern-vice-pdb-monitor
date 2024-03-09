using Avalonia;

namespace Modern.Vice.PdbMonitor.Views.Editor;
internal class ProfilingMargin: AdditionalLineInfoMargin
{
    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(60, 0);
    }
}
