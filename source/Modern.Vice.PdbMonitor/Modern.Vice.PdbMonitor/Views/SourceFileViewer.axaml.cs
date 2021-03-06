using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Modern.Vice.PdbMonitor.Views
{
    // TODO make font settable
    public class SourceFileViewer : UserControl
    {
        int cursorRow;
        int? executionRow;
        double itemHeight;
        /// <summary>
        /// Prevents resetting cursor row when initializing and setting cursor row at the same time
        /// </summary>
        bool settingCursorRow;
        readonly ItemsRepeater lines;
        readonly ScrollViewer scroller;
        public static readonly DirectProperty<SourceFileViewer, int> CursorRowProperty =
            AvaloniaProperty.RegisterDirect<SourceFileViewer, int>(nameof(CursorRow),
                o => o.CursorRow, 
                (o, v) => o.CursorRow = v, 
                defaultBindingMode: BindingMode.TwoWay);
        public static readonly DirectProperty<SourceFileViewer, int?> ExecutionRowProperty =
            AvaloniaProperty.RegisterDirect<SourceFileViewer, int?>(nameof(ExecutionRow),
                o => o.ExecutionRow,
                (o, v) => o.ExecutionRow = v,
                defaultBindingMode: BindingMode.TwoWay);
        public SourceFileViewer()
        {
            InitializeComponent();
            lines = this.Find<ItemsRepeater>("lines");
            scroller = this.Find<ScrollViewer>("scroller");
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
                    CursorRowChanged();
                }
            }
        }
        async Task CursorRowChanged()
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
                            await WaitForLayoutUpdatedAsync();
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
        Task WaitForLayoutUpdatedAsync()
        {
            var tcs = new TaskCompletionSource();
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
        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
