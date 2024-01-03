using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Modern.Vice.PdbMonitor.Converters;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views;

public partial class MemoryViewer : UserControl
{
    readonly HexValueConverter hexValueConverter;
    readonly PetsciiByteToCharConverter petsciiByteToChar;
    BoolToFontWeightConverter ChangedValueToBoldConverter { get; }
    FontFamily c64Font = FontFamily.Default;
    MemoryViewerViewModel? currentViewModel;
    int dataColumnsCount;
    public MemoryViewer()
    {
        InitializeComponent();
        DataContextChanged += MemoryViewer_DataContextChanged;
        hexValueConverter = (HexValueConverter)Resources[nameof(HexValueConverter)].ValueOrThrow();
        petsciiByteToChar = (PetsciiByteToCharConverter)Resources[nameof(PetsciiByteToCharConverter)].ValueOrThrow();
        ChangedValueToBoldConverter = (BoolToFontWeightConverter)Resources[nameof(ChangedValueToBoldConverter)].ValueOrThrow();
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        if (this.TryFindResource("C64Mono", out object? fontResource) && fontResource is FontFamily font)
        {
            c64Font = font;
        }
    }

    private void MemoryViewer_DataContextChanged(object? sender, EventArgs e)
    {
        if (currentViewModel is not null)
        {
            currentViewModel.PropertyChanged -= CurrentViewModel_PropertyChanged;
            RemoveDynamicColumns();
        }
        currentViewModel = (MemoryViewerViewModel?)DataContext;
        if (currentViewModel is not null)
        {
            currentViewModel.PropertyChanged += CurrentViewModel_PropertyChanged;
            dataColumnsCount = currentViewModel.RowSize;
            PopulateDynamicColumns(dataColumnsCount);
        }
    }

    private void CurrentViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (currentViewModel is not null)
        {
            switch (e.PropertyName)
            {
                case nameof(MemoryViewerViewModel.RowSize):
                    RemoveDynamicColumns();
                    PopulateDynamicColumns(currentViewModel.RowSize);
                    break;
            }
        }
    }

    /// <summary>
    /// Removes all but first column
    /// </summary>
    void RemoveDynamicColumns()
    {
        while (grid.Columns.Count > 1)
        {
            grid.Columns.RemoveAt(1);
        }
    }

    void PopulateDynamicColumns(int count)
    {
        PopulateValueColumns(count);
        PopulateCharColumns(count);
    }

    void PopulateValueColumns(int count)
    {
        if (currentViewModel is not null)
        {
            Binding textBinding = new Binding($"{nameof(MemoryViewerCell.Value)}")
            {
                Converter = hexValueConverter,
            };
            Binding previousTextBinding = new Binding($"{nameof(MemoryViewerCell.PreviousValue)}")
            {
                Converter = hexValueConverter,
            };
            Binding fontWeightBinding = new Binding($"{nameof(MemoryViewerCell.HasChanges)}")
            {
                Converter = ChangedValueToBoldConverter,
            };
            MultiBinding previousOpacityBinding = new MultiBinding
            {
                Converter = new FuncMultiValueConverter<bool, double>(x => x.All(y => y) ? 1 : 0),
            };
            previousOpacityBinding.Bindings.Add(new Binding($"{nameof(MemoryViewerCell.HasChanges)}"));
            Binding previousVisibleBinding = new Binding
            {
                Source = currentViewModel,
                Path = nameof(MemoryViewerViewModel.ShowOnlyRowsWithChanges),
            };
            for (int i = 0; i < count; i++)
            {
                Binding dataContextBinding = new Binding($"{nameof(MemoryViewerRow.Cells)}[{i}]");
                string headerText = $"{i:X2}";
                var column = new DataGridTemplateColumn
                {
                    Width = new DataGridLength(24),
                    CellTemplate = new FuncDataTemplate<MemoryViewerRow>((r, ns) =>
                    {
                        var panel = new StackPanel
                        {
                            [!StackPanel.DataContextProperty] = dataContextBinding,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                        };
                        panel.Children.Add(new TextBlock
                        {
                            [!TextBlock.TextProperty] = textBinding,
                            [!TextBlock.FontWeightProperty] = fontWeightBinding,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        });
                        panel.Children.Add(new TextBlock
                        {
                            [!TextBlock.TextProperty] = previousTextBinding,
                            [!OpacityProperty] = previousOpacityBinding,
                            [!IsVisibleProperty] = previousVisibleBinding,
                            Foreground = Brushes.Gray,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        });
                        return panel;
                    }),
                    HeaderTemplate = new FuncDataTemplate<MemoryViewerRow>((r, ns) =>
                    {
                        return new TextBlock
                        {
                            Text = headerText,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                        };
                    }),
                };
                grid.Columns.Add(column);
            }
        }
    }
    void PopulateCharColumns(int count)
    {
        MultiBinding previousOpacityBinding = new MultiBinding
        {
            Converter = new FuncMultiValueConverter<bool, double>(x => x.All(y => y) ? 1 : 0),
        };
        previousOpacityBinding.Bindings.Add(new Binding($"{nameof(MemoryViewerCell.HasChanges)}"));
        Binding previousVisibleBinding = new Binding
        {
            Source = currentViewModel,
            Path = nameof(MemoryViewerViewModel.ShowOnlyRowsWithChanges),
        };
        Binding textBinding = new Binding($"{nameof(MemoryViewerCell.Value)}")
        {
            Converter = petsciiByteToChar,
        };
        Binding previousTextBinding = new Binding($"{nameof(MemoryViewerCell.PreviousValue)}")
        {
            Converter = petsciiByteToChar,
        };
        for (int i = 0; i < count; i++)
        {
            Binding dataContextBinding = new Binding($"{nameof(MemoryViewerRow.Cells)}[{i}]");
            string headerText = $"{i:X2}";
            var column = new DataGridTemplateColumn
            {
                Width = new DataGridLength(12),
                CellTemplate = new FuncDataTemplate<MemoryViewerRow>((r, ns) =>
                {
                    var panel = new StackPanel
                    {
                        [!StackPanel.DataContextProperty] = dataContextBinding,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    panel.Children.Add(new TextBlock
                    {
                        [!TextBlock.TextProperty] = textBinding,
                        FontFamily = c64Font,
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    });
                    panel.Children.Add(new TextBlock
                    {
                        [!TextBlock.TextProperty] = previousTextBinding,
                        FontFamily = c64Font,
                        FontSize = 12,
                        [!OpacityProperty] = previousOpacityBinding,
                        [!IsVisibleProperty] = previousVisibleBinding,
                        Foreground = Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    });
                    return panel;
                }),
                HeaderTemplate = new FuncDataTemplate<MemoryViewerRow>((r, ns) =>
                {
                    return new TextBlock
                    {
                        Text = headerText,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                }),
            };
            grid.Columns.Add(column);
        }
    }
}
