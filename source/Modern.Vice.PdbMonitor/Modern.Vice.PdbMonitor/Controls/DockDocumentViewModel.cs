using Avalonia;
using Dock.Model.Avalonia.Controls;

namespace Modern.Vice.PdbMonitor.Controls;

public class DockDocumentViewModel: Document
{
    public static readonly DirectProperty<DockDocumentViewModel, object?> DataProperty =
        AvaloniaProperty.RegisterDirect<DockDocumentViewModel, object?>(nameof(Data),
            o => o.Data, (o, v) => o.Data = v);
    object? data;
    public object? Data
    {
        get => data;
        set => SetAndRaise(DataProperty, ref data, value);
    }
    public DockDocumentViewModel(object data)
    {
        Data = data;
    }
}
public interface IModernDocumentDock
{
    object Data { get; }
}
