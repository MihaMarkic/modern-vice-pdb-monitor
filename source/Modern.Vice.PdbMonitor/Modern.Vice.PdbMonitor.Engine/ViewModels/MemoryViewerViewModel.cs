using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public class MemoryViewerViewModel: NotifiableObject
{
    readonly ILogger<MemoryViewerViewModel> logger;
    readonly EmulatorMemoryViewModel emulatorMemoryViewModel;
    public int RowSize { get; } = 16;
    public bool ShowOnlyRowsWithChanges { get; set; }
    public ImmutableArray<MemoryViewerRow> Rows { get; private set; }
    public ImmutableArray<MemoryViewerRow> FilteredRows { get; private set; }
    public MemoryViewerViewModel(ILogger<MemoryViewerViewModel> logger, EmulatorMemoryViewModel emulatorMemoryViewModel)
    {
        this.logger = logger;
        this.emulatorMemoryViewModel = emulatorMemoryViewModel;
        emulatorMemoryViewModel.MemoryContentChanged += EmulatorMemoryViewModel_MemoryContentChanged;
        CreateRows();
        FilterRows();
    }

    private void EmulatorMemoryViewModel_MemoryContentChanged(object? sender, EventArgs e)
    {
        FilterRows();
        foreach (var row in FilteredRows)
        {
            row.RaiseChanged();
        }
    }

    private void FilterRows()
    {
        if (ShowOnlyRowsWithChanges)
        {
            FilteredRows = Rows.Where(r => r.Cells.Any(c => c.HasChanges)).ToImmutableArray();
        }
        else
        {
            FilteredRows = Rows;
        }
    }

    internal void CreateRows()
    {
        int allRows = ushort.MaxValue / RowSize;
        var rowsBuilder = ImmutableArray.CreateBuilder<MemoryViewerRow>();
        for (int r = 0; r < allRows; r++)
        {
            ushort start = (ushort)(r * RowSize);
            MemoryViewerCell[] cells = new MemoryViewerCell[RowSize];
            for (int j = 0; j < cells.Length; j++)
            {
                cells[j] = new MemoryViewerCell(emulatorMemoryViewModel, (ushort)(start + j));
            }
            rowsBuilder.Add(new MemoryViewerRow(start, cells.ToImmutableArray()));
        }
        Rows = rowsBuilder.ToImmutable();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        emulatorMemoryViewModel.MemoryContentChanged -= EmulatorMemoryViewModel_MemoryContentChanged;
    }

    protected override void OnPropertyChanged([CallerMemberName] string name = default!)
    {
        base.OnPropertyChanged(name);
        switch (name)
        {
            case nameof(ShowOnlyRowsWithChanges):
                FilterRows();
                break;
        }
    }
}

public class MemoryViewerRow
{
    public ushort Address { get; }
    public ImmutableArray<MemoryViewerCell> Cells { get; }
    public MemoryViewerRow(ushort address, ImmutableArray<MemoryViewerCell> cells)
    {
        Address = address;
        Cells = cells;
    }

    public void RaiseChanged()
    {
        foreach (var cell in Cells)
        {
            cell.RaiseChanged();
        }
    }
}

public class MemoryViewerCell: NotifiableObject
{
    readonly IEmulatorMemory owner;
    public ushort Address { get; }
    public MemoryViewerCell(IEmulatorMemory owner, ushort address)
    {
        this.owner = owner;
        Address = address;
    }
    public byte Value => owner.Current[Address];
    public byte PreviousValue => owner.Previous[Address];
    public bool HasChanges => Value != PreviousValue;
    public void RaiseChanged()
    {
        OnPropertyChanged(nameof(Value));
    }
}
