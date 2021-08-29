using System;
using System.Collections.Immutable;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Modern.Vice.PdbMonitor.Core.Common.Compiler;

namespace Modern.Vice.PdbMonitor.Views
{
    /// <summary>
    /// Provides execution line background for <see cref="SourceFileViewer"/>.
    /// </summary>
    public class LineColorizer : DocumentColorizingTransformer
    {
        public int? LineNumber { get; set; }
        public ImmutableDictionary<int, ImmutableArray<SyntaxElement>> Elements { get; set; } = ImmutableDictionary<int, ImmutableArray<SyntaxElement>>.Empty;

        protected override void ColorizeLine(DocumentLine line)
        {
            if (!line.IsDeleted)
            {
                if (Elements.TryGetValue(line.LineNumber, out var elements))
                {
                    Action<VisualLineElement>? apply;
                    foreach (var element in elements)
                    {
                        apply = element.ElementType switch
                        {
                            SyntaxElementType.String => ApplyStringChanges,
                            SyntaxElementType.Comment => ApplyCommentChanges,
                            _ => null,
                        };
                        if (apply is not null)
                        {
                            ChangeLinePart(Math.Max(line.Offset, element.Start), Math.Min(element.End + 1, line.EndOffset), apply);
                        }
                    }
                }
                if (LineNumber.HasValue && line.LineNumber == LineNumber)
                {
                    ChangeLinePart(line.Offset, line.EndOffset, ApplyExecutionLineChanges);
                }
            }
        }

        void ApplyStringChanges(VisualLineElement element) => element.TextRunProperties.ForegroundBrush = ElementColor.String;
        void ApplyCommentChanges(VisualLineElement element) => element.TextRunProperties.ForegroundBrush = ElementColor.Comment;

        void ApplyExecutionLineChanges(VisualLineElement element)
        {
            // This is where you do anything with the line
            
            element.TextRunProperties.BackgroundBrush = Brushes.Yellow;
        }

        static class ElementColor
        {
            public static readonly IBrush String = Brushes.DarkRed;
            public static readonly IBrush Comment = Brushes.LightGray;
        }
    }
}
