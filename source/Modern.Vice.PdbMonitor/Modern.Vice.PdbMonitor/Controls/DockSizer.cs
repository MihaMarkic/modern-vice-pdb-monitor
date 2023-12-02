using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Modern.Vice.PdbMonitor.Controls;
public class DockSizer : Panel
{
    public static readonly DirectProperty<DockSizer, DockSizerOrientation> OrientationProperty
        = AvaloniaProperty.RegisterDirect<DockSizer, DockSizerOrientation>(
           nameof(Orientation),
           o => o.Orientation,
           (o, v) => o.Orientation = v
        );
    public static readonly DirectProperty<DockSizer, double> MinSizedWidthProperty
    = AvaloniaProperty.RegisterDirect<DockSizer, double>(
       nameof(MinSizedWidth),
       o => o.MinSizedWidth,
       (o, v) => o.MinSizedWidth = v
    );
    DockSizerOrientation orientation = DockSizerOrientation.Horizontal;
    double minSizedWidth = 80;
    Control? related;
    Panel? parent;
    public DockSizerOrientation Orientation
    {
        get => orientation;
        set
        {
            if (value != Orientation)
            {
                SetAndRaise(OrientationProperty, ref orientation, value);
                UpdateCursor();
            }
        }
    }
    public double MinSizedWidth
    {
        get => minSizedWidth;
        set => SetAndRaise(MinSizedWidthProperty, ref minSizedWidth, value);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        UpdateCursor();
    }

    void UpdateCursor()
    {
        Cursor = Orientation switch
        {
            DockSizerOrientation.Vertical => new Cursor(StandardCursorType.SizeWestEast),
            _ => new Cursor(StandardCursorType.SizeNorthSouth)
        };
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        parent = Parent as Panel;
        if (parent is not null)
        {
            int index = parent.Children.IndexOf(this);
            if (index > 0)
            {
                related = parent.Children[index - 1];
                e.Pointer.Capture(this);
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (related is not null && parent is not null)
        {
            var newPosition = e.GetPosition(parent);
            switch (Orientation)
            {
                case DockSizerOrientation.Vertical:
                    double newWidth = Math.Max(MinSizedWidth, newPosition.X);
                    if (newWidth > parent.Bounds.Width + Width)
                    {
                        newWidth = parent.Bounds.Width - Width;
                    }
                    related.Width = newWidth;
                    break;
                case DockSizerOrientation.Horizontal:
                    double newHeight = Math.Max(MinSizedWidth, parent.Bounds.Height - newPosition.Y);
                    if (newHeight > parent.Bounds.Height - Height)
                    {
                        newHeight = parent.Bounds.Height - Height;
                    }
                    related.Height = newHeight;
                    break;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.Pointer.Captured == this)
        {
            e.Pointer.Capture(null);
            parent = null;
            related = null;
        }
    }
}

public enum DockSizerOrientation
{
    Horizontal,
    Vertical
}
