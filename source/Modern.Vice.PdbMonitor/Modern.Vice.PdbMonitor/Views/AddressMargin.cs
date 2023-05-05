using System.Collections.Immutable;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views;

public class AddressMargin : AdditionalLineInfoMargin
{
    readonly double fontSize;
    readonly IBrush foreground;
    readonly ImmutableArray<LineViewModel> lines;
    readonly Typeface typeface;
    public AddressMargin(FontFamily fontFamily, double fontSize, IBrush foreground, ImmutableArray<LineViewModel> lines)
    {
        this.fontSize = fontSize;
        this.foreground = foreground;
        this.lines = lines;
        typeface = new Typeface(fontFamily);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var text = new FormattedText(
            "0000", // max address length is 4 chars
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            foreground
        );
        return new Size(text.Width, 0);
    }
    public override void Render(DrawingContext context)
    {
        var textView = TextView;
        var renderSize = Bounds.Size;
        // necessary to capture pointer
        if (textView != null && textView.VisualLinesValid)
        {
            foreach (var visualLine in textView.VisualLines)
            {
                var lineNumber = visualLine.FirstDocumentLine.LineNumber;
                var line = lines[lineNumber - 1];
                if (line.Address.HasValue)
                {
                    var y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextTop);
                    var text = new FormattedText(
                        line.Address.Value.ToString("X4"),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        fontSize,
                        foreground
                    );
                    context.DrawText(text, new Point(0, y - TextView.VerticalOffset));
                }
            }
        }
    }
}
