using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Dock.Model.Avalonia.Controls;
using Dock.Model.Core;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Controls;
/// <summary>
/// Provides support for binding <see cref="IDockable"/> collection to dock.
/// </summary>
public class ModernDocumentDock : DocumentDock
{
    public static readonly DirectProperty<ModernDocumentDock, IList?> DocumentsProperty =
        AvaloniaProperty.RegisterDirect<ModernDocumentDock, IList?>(nameof(Documents), 
            o => o.Documents, (o, v) => o.Documents = v);
    IList? documents;
    public ModernDocumentDock()
    {
        ((IAvaloniaList<IDockable>)VisibleDockables!).CollectionChanged += VisibleDockables_CollectionChanged; ;
    }
    //[DataMember(IsRequired = false, EmitDefaultValue = true)]
    //[JsonPropertyName("Documents")]
    public IList? Documents
    {
        get => documents;
        set => SetAndRaise(DocumentsProperty, ref documents, value);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == DocumentsProperty)
        {
            if (change.OldValue is INotifyCollectionChanged old)
            {
                old.CollectionChanged -= Documents_CollectionChanged;
            }
            if (change.NewValue is INotifyCollectionChanged newValue)
            {
                newValue.CollectionChanged += Documents_CollectionChanged;
            }
        }
        base.OnPropertyChanged(change);
    }

    void VisibleDockables_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            var document = e.OldItems?.Cast<DockDocumentViewModel>().Select(d => d.Data).Single()!;
            Documents!.Remove(document);
        }
    }

    void Documents_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            if (DocumentTemplate?.Content is null)
            {
                return;
            }

            var data = e.NewItems?.Count == 1 ? e.NewItems[0]: null;
            if (data is not null)
            {
                var document = new DockDocumentViewModel(data);
                if (data is SourceFileViewModel source)
                {
                    document.Title = source.Path.FileName;
                }
                document.Content = DocumentTemplate.Content;
                Factory?.AddDockable(this, document);
                Factory?.SetActiveDockable(document);
                Factory?.SetFocusedDockable(this, document);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            var data = e.OldItems?.Count == 1 ? e.OldItems[0]: null;
            if (data is not null)
            {
                var document = VisibleDockables?.Cast<DockDocumentViewModel>().Where(d => ReferenceEquals(d, data)).FirstOrDefault();
                if (document is not null)
                {
                    Owner!.Factory!.CloseDockable(document);
                }
            }
        }
    }
}
