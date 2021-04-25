using System.Diagnostics;
using System.Linq;
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
        readonly ItemsRepeater lines;
        readonly ScrollViewer scroller;
        public static DirectProperty<SourceFileViewer, int> CursorRowProperty =
            AvaloniaProperty.RegisterDirect<SourceFileViewer, int>(nameof(CursorRow),
                o => o.CursorRow, 
                (o, v) => o.CursorRow = v, 
                defaultBindingMode: BindingMode.TwoWay);
        public SourceFileViewer()
        {
            InitializeComponent();
            lines = this.Find<ItemsRepeater>("lines");
            scroller = this.Find<ScrollViewer>("scroller");
        }

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
        void CursorRowChanged()
        {
            bool success = ScrollToCursorRow();
            if (!success)
            {
                lines.ElementPrepared += Lines_ElementPrepared;
            }
        }

        void Lines_ElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
        {
            if (ScrollToCursorRow())
            {
                lines.ElementPrepared -= Lines_ElementPrepared;
            }
        }

        bool ScrollToCursorRow()
        {
            var firstVisibleChild = lines.GetVisualChildren().FirstOrDefault();
            if (firstVisibleChild is not null)
            {
                double itemHeight = firstVisibleChild.Bounds.Height;
                if (itemHeight > 0)
                {
                    scroller.Offset = scroller.Offset.WithY(itemHeight * (CursorRow-1));
                    return true;
                }
            }
            return false;
        }
        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
