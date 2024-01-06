using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.TextMate;
using AvaloniaEdit.Utils;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Core.Common.Compiler;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using Modern.Vice.PdbMonitor.Views.Editor;
using TextMateSharp.Grammars;
using static System.Net.Mime.MediaTypeNames;

namespace Modern.Vice.PdbMonitor.Views;

public partial class SourceFileViewer : UserControl
{
    LineColorizer lineColorizer = default!;
    LineNumbersMargin? lineNumbersMargin;
    static readonly RegistryOptions registryOptions;
    static readonly PropertyInfo TextEditorScrollViewerPropertyInfo;
    SourceFileViewModel? oldTypedDataContext;
    SourceFileViewModel? oldDataContext;
    BreakpointsMargin? breakpointsMargin;
    AddressMargin? addressMargin;

    bool useLineColorizerForElements;
    TextMate.Installation textMateInstallation;
    SourceFileViewModel? oldViewModel;
    static SourceFileViewer()
    {
        TextEditorScrollViewerPropertyInfo = typeof(TextEditor)
            .GetProperty("ScrollViewer", BindingFlags.Instance | BindingFlags.NonPublic)
            .ValueOrThrow();
        registryOptions = new RegistryOptions(ThemeName.SolarizedLight);
    }
    public SourceFileViewer()
    {
        InitializeComponent();
        textMateInstallation = Editor.InstallTextMate(registryOptions);
        DataContextChanged += SourceFileViewer_DataContextChanged;
    }
    ScrollViewer? EditorScrollViewer => (ScrollViewer?)TextEditorScrollViewerPropertyInfo.GetValue(Editor);
    internal SourceFileViewModel? ViewModel => (SourceFileViewModel?)base.DataContext;
    void SourceFileViewer_DataContextChanged(object? sender, EventArgs e)
    {
        //    if (oldDataContext is not null)
        //    {
        //        oldDataContext.PropertyChanged -= DockDocumentViewModel_PropertyChanged;
        //    }
        BindToViewModel();
        UpdateContent();
        //    var dataContext = DockDocumentViewModel;
        //    if (dataContext is not null)
        //    {
        //        oldDataContext = dataContext;
        //        dataContext.PropertyChanged += DockDocumentViewModel_PropertyChanged;
        //    }
    }
    void BindToViewModel()
    {
        if (oldViewModel is not null)
        {
            oldViewModel.ShowCursorRow -= ViewModel_ShowCursorRow;
            oldViewModel.ShowCursorColumn -= ViewModel_ShowCursorColumn;
            oldViewModel.MoveCaret -= ViewModel_MoveCaret;
            oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            oldViewModel.ExecutionRowChanged -= ViewModel_ExecutionRowChanged;
            oldViewModel.BreakpointsChanged -= ViewModel_BreakpointsChanged;
            oldViewModel.ContentChanged -= ViewModel_ContentChanged;
        }
        var viewModel = ViewModel;
        if (viewModel is not null)
        {
            viewModel.ShowCursorRow += ViewModel_ShowCursorRow;
            viewModel.ShowCursorColumn += ViewModel_ShowCursorColumn;
            viewModel.MoveCaret += ViewModel_MoveCaret;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.ExecutionRowChanged += ViewModel_ExecutionRowChanged;
            viewModel.BreakpointsChanged += ViewModel_BreakpointsChanged;
            viewModel.ContentChanged += ViewModel_ContentChanged;
        }
        oldViewModel = viewModel;
    }

    private void ViewModel_ContentChanged(object? sender, EventArgs e)
    {
        UpdateOnShowAssemblyChange();
    }

    void UpdateOnShowAssemblyChange()
    {
        var viewModel = ViewModel;
        if (viewModel is not null)
        {
            Editor.Text = viewModel.Text;
            lineNumbersMargin?.Update(viewModel.EditorLines, viewModel.EditorRowToLinesMap);
            addressMargin?.Update(viewModel.EditorLines, viewModel.ShowAssemblyLines);
        }
    }

    void UpdateContent()
    {
        var leftMargins = Editor.TextArea.LeftMargins;
        var lineTransformers = Editor.TextArea.TextView.LineTransformers;
        DisconnectOldViewModel(leftMargins, lineTransformers);
        Editor.Text = "";
        Editor.CaretOffset = 0;
        var viewModel = ViewModel;
        if (viewModel is not null)
        {
            lineColorizer = new(viewModel);
            Editor.Text = viewModel.Text;
            breakpointsMargin = new BreakpointsMargin(viewModel);
            leftMargins.Insert(0, breakpointsMargin);
            addressMargin = new AddressMargin(Editor.FontFamily, Editor.FontSize, Brushes.DarkGray, 
                viewModel.EditorLines, viewModel.ShowAssemblyLines)
            {
                Margin = new Thickness(4, 0),
            };
            leftMargins.Insert(1, addressMargin);
            lineNumbersMargin = new LineNumbersMargin(Editor.FontFamily, Editor.FontSize, Brushes.MediumPurple, 
                viewModel.EditorLines, viewModel.EditorRowToLinesMap)
            {
                Margin = new Thickness(4, 0),
            };
            leftMargins.Insert(2, lineNumbersMargin);
            lineColorizer.LineNumber = viewModel.ExecutionRow + 1;
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
        oldTypedDataContext = viewModel;
    }

    private void ViewModel_MoveCaret(object? sender, MoveCaretEventArgs e)
    {
        var caret = Editor.TextArea.Caret;
        caret.Line = e.Line;
        caret.Column = e.Column;
    }

    private void ViewModel_ShowCursorColumn(object? sender, EventArgs e)
    {
        if (ViewModel is not null)
        {
            Editor.ScrollTo(ViewModel.CursorRow, ViewModel.CursorColumn);
        }
    }

    private void Editor_PointerHover(object? sender, PointerEventArgs e)
    {
        if (ViewModel is not null)
        {
            var flyout = (Flyout?)FlyoutBase.GetAttachedFlyout(Editor);
            if (flyout is not null)
            {
                object? symbolReference = GetSymbolReferenceAtPosition(e);
                if (symbolReference is not null)
                {
                    var info = ViewModel.GetContextSymbolReferenceInfo(symbolReference);
                    if (info is not null)
                    {
                        FlyoutContent.DataContext = info;
                        flyout.ShowAt(Editor, true);
                    }
                }
            }
        }
    }

    private void Editor_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel is not null)
        {
            var point = e.GetCurrentPoint(sender as Control);
            if (point.Properties.IsRightButtonPressed)
            {
                ViewModel.ContextSymbolReference = GetSymbolReferenceAtPosition(e);
            }
        }
    }

    object? GetSymbolReferenceAtPosition(PointerEventArgs e)
    {
        if (ViewModel is not null)
        {
            var textView = Editor.TextArea.TextView;
            var pos = e.GetPosition(textView);
            pos = new Point(pos.X, pos.Y.CoerceValue(0, textView.Bounds.Height) + textView.VerticalOffset);
            var vp = textView.GetPosition(pos);
            if (vp is not null)
            {
                Debug.WriteLine($"Pos {vp.Value.Line} {vp.Value.Column}");

                var editorLine = ViewModel.EditorLines[vp.Value.Line - 1];
                if (editorLine is LineViewModel line)
                {
                    Debug.WriteLine($"Line: {line.SourceLine.Text}");
                    var globalVariable = line.SymbolReferences.GlobalVariables.GetAtColumn(vp.Value.Column - 2);
                    if (globalVariable is not null)
                    {
                        Debug.WriteLine($"Found global variable {globalVariable.Name}");
                        return globalVariable;
                    }
                    else
                    {
                        var localVariable = line.SymbolReferences.LocalVariables.GetAtColumn(vp.Value.Column - 1);
                        if (localVariable is not null)
                        {
                            Debug.WriteLine($"Found local variable {localVariable.Name}");
                            return localVariable;
                        }
                        else
                        {
                            var function = line.SymbolReferences.Functions.GetAtColumn(vp.Value.Column - 1);
                            if (function is not null)
                            {
                                Debug.WriteLine($"Found function {function.Name}");
                                return function;
                            }
                        }
                    }
                }
            }
        }
        return null;
    }
    private void Editor_PointerHoverStopped(object? sender, PointerEventArgs e)
    {
    }
    private void Editor_PointerMoved(object? sender, PointerEventArgs e)
    {
        
    }

    void ViewModel_BreakpointsChanged(object? sender, EventArgs e)
    {
        Editor.TextArea.TextView.Redraw();
    }

    void DisconnectOldViewModel(ObservableCollection<Control> leftMargins, IList<IVisualLineTransformer> lineTransformers)
    {
        if (oldTypedDataContext is not null)
        {
            oldTypedDataContext.ShowCursorRow -= ViewModel_ShowCursorRow;
            oldTypedDataContext.PropertyChanged -= ViewModel_PropertyChanged;
            oldTypedDataContext.ExecutionRowChanged -= ViewModel_ExecutionRowChanged;

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
        lineColorizer.LineNumber = ViewModel!.ExecutionRow + 1;
        if (lineColorizer.LineNumber is not null)
        {
            Editor.ScrollTo(lineColorizer.LineNumber.Value, 0);
        }
        Editor.TextArea.TextView.LineTransformers.Add(lineColorizer);
    }

    void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ViewModel.ExecutionRow):
                break;
            case nameof(ViewModel.Elements):
                if (useLineColorizerForElements)
                {
                    lineColorizer.Elements = ViewModel!.Elements;
                    Editor.TextArea.TextView.Redraw();
                }
                break;
        }
    }

    void ViewModel_ShowCursorRow(object? sender, EventArgs e)
    {
        _ = CursorRowChanged(ViewModel!.CursorRow);
    }

    CancellationTokenSource? cts;
    async Task CursorRowChanged(int cursorRow)
    {
        cts?.Cancel();
        if (Editor.Bounds.Height <= 0)
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
        //if (DockDocumentViewModel is not null)
        //{
        //    DockDocumentViewModel.PropertyChanged -= DockDocumentViewModel_PropertyChanged;
        //}
        base.OnDetachedFromVisualTree(e);
    }
}
