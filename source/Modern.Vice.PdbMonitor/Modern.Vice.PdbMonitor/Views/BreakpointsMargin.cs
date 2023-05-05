using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Utils;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views;

/// <summary>
/// Provides breakpoints display and toggle for <see cref="SourceFileViewer"/>.
/// </summary>
public class BreakpointsMargin : AdditionalLineInfoMargin
{
    // diameter of breakpoint icon
    const double D = 14;
    static IBrush activeBrush = Brushes.Red;
    static IBrush hoverBrush = Brushes.LightGray;
    readonly SourceFileViewModel sourceFileViewModel;
    int? hoverLine;
    public BreakpointsMargin(SourceFileViewModel sourceFileViewModel)
    {
        this.sourceFileViewModel = sourceFileViewModel;
        sourceFileViewModel.PropertyChanged += SourceFileViewModel_PropertyChanged;
        sourceFileViewModel.BreakpointsChanged += SourceFileViewModel_BreakpointsChanged;
        Margin = new Thickness(4, 0);
    }

    void SourceFileViewModel_BreakpointsChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    void SourceFileViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SourceFileViewModel.Lines):
                InvalidateVisual();
                break;
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(D, 0);
    }
    /// <inheritdoc/>
    public override void Render(DrawingContext drawingContext)
    {
        var textView = TextView;
        var renderSize = Bounds.Size;
        // necessary to capture pointer
        drawingContext.FillRectangle(Brushes.Transparent, new Rect(new Point(0, 0), renderSize));
        if (textView != null && textView.VisualLinesValid)
        {
            foreach (var visualLine in textView.VisualLines)
            {
                var lineNumber = visualLine.FirstDocumentLine.LineNumber;
                var line = sourceFileViewModel.Lines[lineNumber - 1];
                if (line.HasBreakpoint)
                {
                    DrawBreakpointIcon(drawingContext, activeBrush, visualLine);
                }
                else if (lineNumber == hoverLine && sourceFileViewModel.AddOrRemoveBreakpointCommand.CanExecute(line))
                {
                    DrawBreakpointIcon(drawingContext, hoverBrush, visualLine);
                }
            }
        }
    }

    void DrawBreakpointIcon(DrawingContext drawingContext, IBrush brush, VisualLine visualLine)
    {
        var y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextMiddle);
        drawingContext.DrawGeometry(brush, null, new EllipseGeometry(new Rect(0, y - D / 2 - TextView.VerticalOffset, D, D)));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.Handled && TextView is not null && TextArea is not null)
        {
            var visualLine = GetTextLineSegment(e);
            if (visualLine is not null)
            {
                var lineNumber = visualLine.FirstDocumentLine.LineNumber;
                var line = sourceFileViewModel.Lines[lineNumber-1];
                if (sourceFileViewModel.AddOrRemoveBreakpointCommand.CanExecute(line))
                {
                    sourceFileViewModel.AddOrRemoveBreakpointCommand.Execute(line);
                }
            }
        }
        e.Handled = true;
    }
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        UpdateHoverPosition(e);
    }        
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        UpdateHoverPosition(e);
        bool cursorSet = false;
        if (!e.Handled && TextView is not null && TextArea is not null)
        {
            var visualLine = GetTextLineSegment(e);
            if (visualLine is not null)
            {
                var lineNumber = visualLine.FirstDocumentLine.LineNumber;
                var line = sourceFileViewModel.Lines[lineNumber - 1];
                if (sourceFileViewModel.AddOrRemoveBreakpointCommand.CanExecute(line))
                {
                    Cursor = new Cursor(StandardCursorType.Arrow);
                    cursorSet = true;
                }
            }
        }
        if (!cursorSet)
        {
            Cursor = new Cursor(StandardCursorType.Ibeam);
        }
    }
    void UpdateHoverPosition(PointerEventArgs e)
    {
        if (!e.Handled && TextView is not null && TextArea is not null)
        {
            var visualLine = GetTextLineSegment(e);
            if (visualLine is not null)
            {
                int newHoverLine = visualLine.FirstDocumentLine.LineNumber;
                if (newHoverLine != hoverLine)
                {
                    hoverLine = newHoverLine;
                    InvalidateVisual();
                }
            }
        }
    }
    protected override void OnPointerExited(PointerEventArgs e)
    {
        hoverLine = null;
        base.OnPointerExited(e);
        InvalidateVisual();
    }
    private VisualLine? GetTextLineSegment(PointerEventArgs e)
    {
        var pos = e.GetPosition(TextView);
        pos = new Point(0, pos.Y.CoerceValue(0, TextView.Bounds.Height) + TextView.VerticalOffset);
        var vl = TextView.GetVisualLineFromVisualTop(pos.Y);
        return vl;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        sourceFileViewModel.PropertyChanged -= SourceFileViewModel_PropertyChanged;
        sourceFileViewModel.BreakpointsChanged -= SourceFileViewModel_BreakpointsChanged;
        base.OnDetachedFromVisualTree(e);
    }

}
