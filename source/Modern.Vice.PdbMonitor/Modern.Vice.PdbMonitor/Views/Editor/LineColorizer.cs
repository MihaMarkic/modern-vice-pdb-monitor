using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Modern.Vice.PdbMonitor.Core.Common.Compiler;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views.Editor;

/// <summary>
/// Provides execution line background for <see cref="SourceFileViewer"/>.
/// </summary>
public class LineColorizer : DocumentColorizingTransformer
{
    public int? LineNumber { get; set; }
    readonly SourceFileViewModel sourceFileViewModel;
    public ImmutableDictionary<int, ImmutableArray<SyntaxElement>> Elements { get; set; }
    public ImmutableHashSet<int> CallStackLineNumbers { get; set; }
    public LineColorizer(SourceFileViewModel sourceFileViewModel)
    {
        Elements = ImmutableDictionary<int, ImmutableArray<SyntaxElement>>.Empty;
        CallStackLineNumbers = ImmutableHashSet<int>.Empty;
        this.sourceFileViewModel = sourceFileViewModel;
    }
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
            bool isBackgroundAssigned = false;

            var sourceLine = sourceFileViewModel.EditorLines[line.LineNumber - 1];
            //if (LineNumber.HasValue && sourceLine is LineViewModel)
            //{
            //    int lineIndex = sourceFileViewModel.GetLineIndex(line.LineNumber - 1);
            //    if (LineNumber == lineIndex + 1)
            //    {
            //        ChangeLinePart(line.Offset, line.EndOffset, ApplyExecutionLineChanges);
            //        isBackgroundAssigned = true;
            //    }
            //}
            if (LineNumber.HasValue && LineNumber == line.LineNumber)
            {
                ChangeLinePart(line.Offset, line.EndOffset, ApplyExecutionLineChanges);
                isBackgroundAssigned = true;
            }
            if (!isBackgroundAssigned)
            {
                switch (sourceLine)
                {
                    case LineViewModel lineViewModel:
                        if (lineViewModel.HasBreakpoint)
                        {
                            ChangeLinePart(line.Offset, line.EndOffset, ApplyBreakpointLineChanges);
                        }
                        else
                        {
                            int lineIndex = sourceFileViewModel.GetLineIndex(line.LineNumber - 1);
                            if (CallStackLineNumbers.Contains(lineIndex + 1))
                            {
                                ChangeLinePart(line.Offset, line.EndOffset, ApplyCallStackChanges);
                            }
                        }

                        break;
                    default:
                        ChangeLinePart(line.Offset, line.EndOffset, ApplyAssemblyChanges);
                        break;
                }
            }
        }
    }

    void ApplyStringChanges(VisualLineElement element) => element.TextRunProperties.SetForegroundBrush(ElementColor.String);
    void ApplyCommentChanges(VisualLineElement element) => element.TextRunProperties.SetForegroundBrush(ElementColor.Comment);
    void ApplyAssemblyChanges(VisualLineElement element) => element.TextRunProperties.SetForegroundBrush(ElementColor.Assembly);
    void ApplyCallStackChanges(VisualLineElement element) => element.TextRunProperties.SetBackgroundBrush(ElementColor.CallStackCall);
    void ApplyExecutionLineChanges(VisualLineElement element)
    {
        // This is where you do anything with the line

        element.TextRunProperties.SetForegroundBrush(Brushes.Black);
        element.TextRunProperties.SetBackgroundBrush(Brushes.Yellow);
    }
    void ApplyBreakpointLineChanges(VisualLineElement element)
    {
        // This is where you do anything with the line

        element.TextRunProperties.SetForegroundBrush(Brushes.White);
        element.TextRunProperties.SetBackgroundBrush(ElementColor.BreakpointBackground);
    }

    static class ElementColor
    {
        public static readonly IBrush String = Brushes.DarkRed;
        public static readonly IBrush Comment = Brushes.LightGray;
        public static readonly IBrush Assembly = Brushes.Gray;
        public static readonly IBrush BreakpointBackground = new SolidColorBrush(new Color(0xFF, 0x96, 0x3A, 0x46));
        public static readonly IBrush CallStackCall = new SolidColorBrush(new Color(0x50, 0xB4, 0xE4, 0xB4));
    }
}
