using Dock.Model.Avalonia.Controls;
using Modern.Vice.PdbMonitor.Core;

namespace Modern.Vice.PdbMonitor.Controls;

public class DockDocumentViewModel: Document
{
    public object Data { get; }
    public DockDocumentViewModel(object data)
    {
        Data = data;
    }
}
public interface IModernDocumentDock
{
    object Data { get; }
}
