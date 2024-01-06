using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using Modern.Vice.PdbMonitor.Core.Extensions;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views.Editor;

public class LineNumbersMargin : AdditionalLineInfoMargin
{
    readonly double fontSize;
    readonly IBrush foreground;
    ImmutableArray<EditorLineViewModel> lines;
    ImmutableDictionary<int, int>? linesMap;
    readonly Typeface typeface;
    public LineNumbersMargin(FontFamily fontFamily, double fontSize, IBrush foreground, 
        ImmutableArray<EditorLineViewModel> lines, ImmutableDictionary<int, int>? linesMap)
    {
        this.fontSize = fontSize;
        this.foreground = foreground;
        this.lines = lines;
        this.linesMap = linesMap;
        typeface = new Typeface(fontFamily);
    }
    internal void Update(ImmutableArray<EditorLineViewModel> lines, ImmutableDictionary<int, int>? linesMap)
    {
        this.lines = lines;
        this.linesMap = linesMap;
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        int maxNumber = lines.OfType<LineViewModel>().Count();

        var text = new FormattedText(
            new string('0', maxNumber.CalculateNumberOfDigits()), // max address length is 4 chars
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
                if (line is LineViewModel)
                {
                    int lineIndex = linesMap is not null ? linesMap[lineNumber - 1]+1 : lineNumber;
                    var y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextTop);
                    var text = new FormattedText(
                        lineIndex.ToString(),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        fontSize,
                        foreground
                    );
                    context.DrawText(text, new Point(renderSize.Width - text.Width, y - TextView.VerticalOffset));
                }
            }
        }
    }
}
