using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace Modern.Vice.PdbMonitor.Views
{
    /// <summary>
    /// Provides execution line background for <see cref="SourceFileViewer"/>.
    /// </summary>
    public class LineColorizer : DocumentColorizingTransformer
    {
        public int? LineNumber { get; set; }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (LineNumber.HasValue && !line.IsDeleted && line.LineNumber == LineNumber)
            {
                ChangeLinePart(line.Offset, line.EndOffset, ApplyChanges);
            }
        }

        void ApplyChanges(VisualLineElement element)
        {
            // This is where you do anything with the line
            element.TextRunProperties.BackgroundBrush = Brushes.Yellow;
        }
    }
}
