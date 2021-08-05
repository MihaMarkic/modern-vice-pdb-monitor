using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views
{
    public partial class SourceFileViewer : UserControl
    {
        readonly TextEditor editor;
        static readonly PropertyInfo TextEditorScrollViewerPropertyInfo;
        SourceFileViewModel? oldDataContext;
        static SourceFileViewer()
        {
            TextEditorScrollViewerPropertyInfo = typeof(TextEditor).GetProperty("ScrollViewer", BindingFlags.Instance | BindingFlags.NonPublic)!;
        }
        public SourceFileViewer()
        {
            InitializeComponent();
            editor = this.FindControl<TextEditor>("Editor");
            DataContextChanged += SourceFileViewer_DataContextChanged;
        }
        ScrollViewer? EditorScrollViewer => (ScrollViewer?)TextEditorScrollViewerPropertyInfo.GetValue(editor);
        public new SourceFileViewModel? DataContext => (SourceFileViewModel?)base.DataContext;
        void SourceFileViewer_DataContextChanged(object? sender, System.EventArgs e)
        {
            if (oldDataContext is not null)
            {
                oldDataContext.ShowCursorRow -= ViewModel_ShowCursorRow;
            }
            editor.Text = "";
            editor.CaretOffset = 0;
            var viewModel = DataContext;
            if (viewModel is not null)
            {
                string text = string.Join(Environment.NewLine, viewModel.Lines.Select(l => l.Content));
                editor.Text = text;
                viewModel.ShowCursorRow += ViewModel_ShowCursorRow;
            }
            oldDataContext = viewModel;
        }

        void ViewModel_ShowCursorRow(object? sender, EventArgs e)
        {
            _ = CursorRowChanged(DataContext!.CursorRow);
        }

        CancellationTokenSource? cts;
        async Task CursorRowChanged(int cursorRow)
        {
            cts?.Cancel();
            if (double.IsNaN(editor.Height))
            {
                cts = new CancellationTokenSource();
                var ct = cts.Token;
                await WaitForLayoutUpdatedAsync();
                if (ct.IsCancellationRequested)
                {
                    return;
                }
            }
            ScrollToLine(cursorRow+1);
        }
        Task WaitForLayoutUpdatedAsync()
        {
            var tcs = new TaskCompletionSource();
            EventHandler<AvaloniaPropertyChangedEventArgs> propertyChangedHandler = default!;
            propertyChangedHandler = (s, e) =>
            {
                if (e.Property == BoundsProperty)
                {
                    editor.PropertyChanged -= propertyChangedHandler;
                    tcs.SetResult();
                }
            };
            editor.PropertyChanged += propertyChangedHandler;
            return tcs.Task;
        }
        public void ScrollToLine(int line)
        {
            ScrollTo(line, -1);
        }
        public void ScrollTo(int line, int column)
        {
            const double MinimumScrollFraction = 0.3;
            ScrollTo(line, column, VisualYPosition.LineMiddle, EditorScrollViewer is not null ? editor.ViewportHeight / 2 : 0.0, MinimumScrollFraction);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        /// <param name="column"></param>
        /// <param name="yPositionMode"></param>
        /// <param name="referencedVerticalViewPortOffset"></param>
        /// <param name="minimumScrollFraction"></param>
        /// <remarks>This code is commented out in AvaloniaEdit, similar reimplementation here.
        /// It uses a bit of reflection since TextEditor.ScrollViewer is internal.</remarks>
        public void ScrollTo(int line, int column, VisualYPosition yPositionMode,
            double referencedVerticalViewPortOffset, double minimumScrollFraction)
        {
            TextView textView = editor.TextArea.TextView;
            var document = textView.Document!;
            var scrollViewer = EditorScrollViewer;
            if (scrollViewer is not null && document is not null)
            {
                if (line < 1)
                    line = 1;
                if (line > document.LineCount)
                    line = document.LineCount;

                // Word wrap is enabled. Ensure that we have up-to-date info about line height so that we scroll
                // to the correct position.
                // This avoids that the user has to repeat the ScrollTo() call several times when there are very long lines.
                VisualLine vl = textView.GetOrConstructVisualLine(document.GetLineByNumber(line));
                double remainingHeight = referencedVerticalViewPortOffset;

                while (remainingHeight > 0)
                {
                    var prevLine = vl.FirstDocumentLine.PreviousLine;
                    if (prevLine == null)
                        break;
                    vl = textView.GetOrConstructVisualLine(prevLine);
                    remainingHeight -= vl.Height;
                }

                Point p = editor.TextArea.TextView.GetVisualPosition(new TextViewPosition(line, Math.Max(1, column)),
                    yPositionMode);
                double verticalPos = p.Y - referencedVerticalViewPortOffset;
                if (Math.Abs(verticalPos - textView.VerticalOffset) >
                    minimumScrollFraction * editor.ViewportHeight)
                {
                    scrollViewer.Offset = new Vector(0, Math.Max(0, verticalPos));
                }

                //if (column > 0)
                //{
                //    if (p.X > editor.ViewportWidth - Caret.MinimumDistanceToViewBorder * 2)
                //    {
                //        double horizontalPos = Math.Max(0, p.X - scrollViewer.ViewportWidth / 2);
                //        if (Math.Abs(horizontalPos - scrollViewer.HorizontalOffset) >
                //            minimumScrollFraction * scrollViewer.ViewportWidth)
                //        {
                //            scrollViewer.ScrollToHorizontalOffset(horizontalPos);
                //        }
                //    }
                //    else
                //    {
                //        scrollViewer.ScrollToHorizontalOffset(0);
                //    }
                //}
            }
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
