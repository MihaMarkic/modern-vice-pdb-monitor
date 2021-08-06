using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using System;
using System.Globalization;

namespace Modern.Vice.PdbMonitor.Views
{
    /// <summary>
    /// Provides UI update trigger upon document or text changes.
    /// </summary>
    public abstract class AdditionalLineInfoMargin : AbstractMargin
    {
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
    }
}
