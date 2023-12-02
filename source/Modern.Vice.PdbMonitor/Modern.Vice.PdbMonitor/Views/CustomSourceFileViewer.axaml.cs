using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.VisualTree;

namespace Modern.Vice.PdbMonitor.Views;

// TODO make font settable
partial class CustomSourceFileViewer : UserControl
{
    int cursorRow;
    int? executionRow;
    double itemHeight;
    /// <summary>
    /// Prevents resetting cursor row when initializing and setting cursor row at the same time
    /// </summary>
    bool settingCursorRow;
    public static readonly DirectProperty<CustomSourceFileViewer, int> CursorRowProperty =
        AvaloniaProperty.RegisterDirect<CustomSourceFileViewer, int>(nameof(CursorRow),
            o => o.CursorRow, 
            (o, v) => o.CursorRow = v, 
            defaultBindingMode: BindingMode.TwoWay);
    public static readonly DirectProperty<CustomSourceFileViewer, int?> ExecutionRowProperty =
        AvaloniaProperty.RegisterDirect<CustomSourceFileViewer, int?>(nameof(ExecutionRow),
            o => o.ExecutionRow,
            (o, v) => o.ExecutionRow = v,
            defaultBindingMode: BindingMode.TwoWay);
    public CustomSourceFileViewer()
    {
        InitializeComponent();
        scroller.ScrollChanged += Scroller_ScrollChanged;
    }

    void Scroller_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        var firstVisibleChild = lines.GetVisualChildren().FirstOrDefault();
        // TODO find a way to determine current top line
        //if (firstVisibleChild is not null)
        //{
        //    cursorRow = lines.GetElementIndex((IControl)firstVisibleChild);
        //}
        //else
        // don't clear it until it isn't set
        if (!settingCursorRow)
        {
            cursorRow = -1;
        }
    }

    public int? ExecutionRow
    {
        get => executionRow;
        set
        {
            if (ExecutionRow != value)
            {
                SetAndRaise(ExecutionRowProperty, ref executionRow, value);
                ExecutionRowChanged();
            }
        }
    }
    void ExecutionRowChanged()
    {
        //bool success = ScrollToCursorRow();
        //if (!success)
        //{
        //    lines.ElementPrepared += Lines_ElementPrepared;
        //}
    }
    CancellationTokenSource? cursorRowChangedCts;
    /// <summary>
    /// Row to jump to.
    /// </summary>
    public int CursorRow
    {
        get => cursorRow;
        set
        {
            if (CursorRow != value)
            {
                SetAndRaise(CursorRowProperty, ref cursorRow, value);
                cursorRowChangedCts?.Cancel();
                cursorRowChangedCts = new CancellationTokenSource();
                _ = CursorRowChangedAsync(cursorRowChangedCts.Token);
            }
        }
    }
    async Task CursorRowChangedAsync(CancellationToken ct)
    {
        if (CursorRow >= 0)
        {
            bool success = ScrollToCursorRow();
            if (!success)
            {
                settingCursorRow = true;
                try
                {
                    while (!success)
                    {
                        await WaitForLayoutUpdatedAsync(ct);
                        success = ScrollToCursorRow();
                    }
                }
                finally
                {
                    settingCursorRow = false;
                }
            }
        }
    }
    Task WaitForLayoutUpdatedAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource();
        ct.Register(tcs.SetCanceled);
        EventHandler layoutUpdatedHandler = default!;
        layoutUpdatedHandler = (s, e) =>
        {
            lines.LayoutUpdated -= layoutUpdatedHandler;
            tcs.SetResult();
        };
        lines.LayoutUpdated += layoutUpdatedHandler;
        return tcs.Task;
    }

    void CalculateItemHeight()
    {
        if (itemHeight > 0)
        {
            return;
        }
        var firstVisibleChild = lines.GetVisualChildren().FirstOrDefault();
        if (firstVisibleChild is not null)
        {
            itemHeight = firstVisibleChild.Bounds.Height;
        }
    }
    bool ScrollToCursorRow()
    {
        //if (CursorRow == actualCursorRow)
        //{
        //    return true;
        //}
        if (itemHeight == 0)
        {
            CalculateItemHeight();
        }
        if (itemHeight > 0)
        {
            scroller.Offset = scroller.Offset.WithY(itemHeight * (CursorRow-1));
            return true;
        }
        return false;
    }
}
