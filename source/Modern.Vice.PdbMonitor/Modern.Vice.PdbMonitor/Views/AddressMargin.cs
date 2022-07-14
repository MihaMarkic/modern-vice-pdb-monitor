using System.Collections.Immutable;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Utils;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views;

public class AddressMargin : AdditionalLineInfoMargin
{
    readonly FontFamily typeface;
    readonly double fontSize;
    readonly IBrush foreground;
    readonly ImmutableArray<LineViewModel> lines;
    public AddressMargin(FontFamily typeface, double fontSize, IBrush foreground, ImmutableArray<LineViewModel> lines)
    {
        this.typeface = typeface;
        this.fontSize = fontSize;
        this.foreground = foreground;
        this.lines = lines;
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var text = TextFormatterFactory.CreateFormattedText(
            this,
            "0000", // max address length is 4 chars
            typeface,
            fontSize,
            foreground
        );
        return new Size(text.Bounds.Width, 0);
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
                    var text = TextFormatterFactory.CreateFormattedText(
                        this,
                        line.Address.Value.ToString("X4"),
                        typeface, fontSize, foreground
                    );
                    context.DrawText(foreground, new Point(0, y - TextView.VerticalOffset), text);
                }
            }
        }
    }
}
