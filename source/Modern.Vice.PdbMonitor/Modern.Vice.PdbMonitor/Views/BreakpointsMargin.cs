﻿using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Utils;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views
{
    public class BreakpointsMargin : AbstractMargin
    {
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
        protected override void OnPointerEnter(PointerEventArgs e)
        {
            base.OnPointerEnter(e);
            UpdateHoverPosition(e);
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            UpdateHoverPosition(e);
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
        protected override void OnPointerLeave(PointerEventArgs e)
        {
            hoverLine = null;
            base.OnPointerLeave(e);
            InvalidateVisual();
        }
        private VisualLine? GetTextLineSegment(PointerEventArgs e)
        {
            var pos = e.GetPosition(TextView);
            pos = new Point(0, pos.Y.CoerceValue(0, TextView.Bounds.Height) + TextView.VerticalOffset);
            var vl = TextView.GetVisualLineFromVisualTop(pos.Y);
            return vl;
        }

        /// <inheritdoc/>
		protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
        {
            if (oldTextView != null)
            {
                oldTextView.VisualLinesChanged -= TextViewVisualLinesChanged;
            }
            base.OnTextViewChanged(oldTextView, newTextView);
            if (newTextView != null)
            {
                newTextView.VisualLinesChanged += TextViewVisualLinesChanged;
            }
            InvalidateVisual();
        }
        /// <inheritdoc/>
        protected override void OnDocumentChanged(TextDocument oldDocument, TextDocument newDocument)
        {
            if (oldDocument != null)
            {
                TextDocumentWeakEventManager.LineCountChanged.RemoveHandler(oldDocument, OnDocumentLineCountChanged);
            }
            base.OnDocumentChanged(oldDocument, newDocument);
            if (newDocument != null)
            {
                TextDocumentWeakEventManager.LineCountChanged.AddHandler(newDocument, OnDocumentLineCountChanged);
            }
            OnDocumentLineCountChanged();
        }
        void OnDocumentLineCountChanged(object? sender, EventArgs e)
        {
            OnDocumentLineCountChanged();
        }

        void TextViewVisualLinesChanged(object? sender, EventArgs e)
        {
            InvalidateMeasure();
        }
        /// <summary>
        /// Maximum length of a line number, in characters
        /// </summary>
        protected int MaxLineNumberLength = 1;

        private void OnDocumentLineCountChanged()
        {
            var documentLineCount = Document?.LineCount ?? 1;
            var newLength = documentLineCount.ToString(CultureInfo.CurrentCulture).Length;

            // The margin looks too small when there is only one digit, so always reserve space for
            // at least two digits
            if (newLength < 2)
                newLength = 2;

            if (newLength != MaxLineNumberLength)
            {
                MaxLineNumberLength = newLength;
                InvalidateMeasure();
            }
        }
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            sourceFileViewModel.PropertyChanged -= SourceFileViewModel_PropertyChanged;
            sourceFileViewModel.BreakpointsChanged -= SourceFileViewModel_BreakpointsChanged;
            base.OnDetachedFromVisualTree(e);
        }

    }
}