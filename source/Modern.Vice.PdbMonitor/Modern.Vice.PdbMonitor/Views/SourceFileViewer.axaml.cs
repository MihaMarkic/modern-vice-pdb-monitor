using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.TextMate;
using Modern.Vice.PdbMonitor.Controls;
using Modern.Vice.PdbMonitor.Core.Common.Compiler;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using TextMateSharp.Grammars;

namespace Modern.Vice.PdbMonitor.Views;

public partial class SourceFileViewer : UserControl
{
    LineColorizer lineColorizer = default!;
    static readonly RegistryOptions registryOptions;
    static readonly PropertyInfo TextEditorScrollViewerPropertyInfo;
    SourceFileViewModel? oldDataContext;
    bool useLineColorizerForElements;
    TextMate.Installation textMateInstallation;
    static SourceFileViewer()
    {
        TextEditorScrollViewerPropertyInfo = typeof(TextEditor).GetProperty("ScrollViewer", BindingFlags.Instance | BindingFlags.NonPublic)!;
        registryOptions = new RegistryOptions(ThemeName.SolarizedLight);
    }
    public SourceFileViewer()
    {
        InitializeComponent();
        textMateInstallation = Editor.InstallTextMate(registryOptions);
        DataContextChanged += SourceFileViewer_DataContextChanged;
    }
    ScrollViewer? EditorScrollViewer => (ScrollViewer?)TextEditorScrollViewerPropertyInfo.GetValue(Editor);
    public new SourceFileViewModel? DataContext => (SourceFileViewModel?)((DockDocumentViewModel?)base.DataContext)?.Data;
    void SourceFileViewer_DataContextChanged(object? sender, EventArgs e)
    {
        var leftMargins = Editor.TextArea.LeftMargins;
        var lineTransformers = Editor.TextArea.TextView.LineTransformers;
        DisconnectOldViewModel(leftMargins, lineTransformers);
        Editor.Text = "";
        Editor.CaretOffset = 0;
        var viewModel = DataContext;
        if (viewModel is not null)
        {
            lineColorizer = new(viewModel);
            string text = string.Join(Environment.NewLine, viewModel.Lines.Select(l => l.Content));
            Editor.Text = text;
            viewModel.ShowCursorRow += ViewModel_ShowCursorRow;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.ExecutionRowChanged += ViewModel_ExecutionRowChanged;
            viewModel.BreakpointsChanged += ViewModel_BreakpointsChanged;
            var breakpointsMargin = new BreakpointsMargin(viewModel);
            leftMargins.Insert(0, breakpointsMargin);
            var addressMargin = new AddressMargin(Editor.FontFamily, Editor.FontSize, Brushes.DarkGray, viewModel.Lines)
            {
                Margin = new Thickness(4, 0),
            };
            leftMargins.Insert(1, addressMargin);
            lineColorizer.LineNumber = viewModel.ExecutionRow;
            Editor.TextArea.TextView.LineTransformers.Add(lineColorizer);
            if (viewModel.SourceLanguage == SourceLanguage.Custom)
            {
                useLineColorizerForElements = true;
                if (!viewModel.Elements.IsEmpty)
                {
                    lineColorizer.Elements = viewModel.Elements;
                }
            }
            else
            {
                switch (viewModel.SourceLanguage)
                {
                    case SourceLanguage.C:
                        string languageId = registryOptions.GetLanguageByExtension(".cs").Id;
                        string scopeName = registryOptions.GetScopeByLanguageId(languageId);
                        textMateInstallation.SetGrammar(scopeName);
                        break;
                }
            }
        }
        oldDataContext = viewModel;
    }

    void ViewModel_BreakpointsChanged(object? sender, EventArgs e)
    {
        Editor.TextArea.TextView.Redraw();
    }

    void DisconnectOldViewModel(ObservableCollection<Control> leftMargins, IList<IVisualLineTransformer> lineTransformers)
    {
        if (oldDataContext is not null)
        {
            oldDataContext.ShowCursorRow -= ViewModel_ShowCursorRow;
            oldDataContext.PropertyChanged -= ViewModel_PropertyChanged;
            oldDataContext.ExecutionRowChanged -= ViewModel_ExecutionRowChanged;

            foreach (var lm in leftMargins.ToImmutableArray())
            {
                if (lm is BreakpointsMargin || lm is AddressMargin)
                {
                    leftMargins.Remove(lm);
                }
            }
            foreach (var lc in lineTransformers.ToImmutableArray())
            {
                if (ReferenceEquals(lc, lineColorizer))
                {
                    lineTransformers.Remove(lc);
                }
            }
            useLineColorizerForElements = false;
        }
    }

    private void ViewModel_ExecutionRowChanged(object? sender, EventArgs e)
    {
        Editor.TextArea.TextView.LineTransformers.Remove(lineColorizer);
        lineColorizer.LineNumber = DataContext!.ExecutionRow + 1;
        Editor.TextArea.TextView.LineTransformers.Add(lineColorizer);
    }

    void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DataContext.ExecutionRow):
                break;
            case nameof(DataContext.Elements):
                if (useLineColorizerForElements)
                {
                    lineColorizer.Elements = DataContext!.Elements;
                    Editor.TextArea.TextView.Redraw();
                }
                break;
        }
    }

    void ViewModel_ShowCursorRow(object? sender, EventArgs e)
    {
        _ = CursorRowChanged(DataContext!.CursorRow);
    }

    CancellationTokenSource? cts;
    async Task CursorRowChanged(int cursorRow)
    {
        cts?.Cancel();
        if (double.IsNaN(Editor.Height))
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
                Editor.PropertyChanged -= propertyChangedHandler;
                tcs.SetResult();
            }
        };
        Editor.PropertyChanged += propertyChangedHandler;
        return tcs.Task;
    }
    public void ScrollToLine(int line)
    {
        ScrollTo(line, -1);
    }
    public void ScrollTo(int line, int column)
    {
        const double MinimumScrollFraction = 0.3;
        ScrollTo(line, column, VisualYPosition.LineMiddle, EditorScrollViewer is not null ? Editor.ViewportHeight / 2 : 0.0, MinimumScrollFraction);
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
        TextView textView = Editor.TextArea.TextView;
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

            Point p = Editor.TextArea.TextView.GetVisualPosition(new TextViewPosition(line, Math.Max(1, column)),
                yPositionMode);
            double verticalPos = p.Y - referencedVerticalViewPortOffset;
            if (Math.Abs(verticalPos - textView.VerticalOffset) >
                minimumScrollFraction * Editor.ViewportHeight)
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

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        textMateInstallation.Dispose();
        base.OnDetachedFromVisualTree(e);
    }
}
