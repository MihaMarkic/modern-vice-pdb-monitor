using System.Runtime.CompilerServices;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Models;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class BreakpointViewModel : NotifiableObject, ICloneable
{
    public bool IsCurrentlyHit { get; set; }
    public bool StopWhenHit { get; set; }
    public bool IsEnabled { get; set; }
    public uint HitCount { get; set; }
    public uint IgnoreCount { get; set; }
    public BreakpointMode Mode { get; set; }
    public BreakpointBind Bind { get; set; }
    public string? Condition { get; set; }
    /// <summary>
    /// Flag that signals breakpoint not enabled due to errors, i.e. global variable not found.
    /// </summary>
    public bool HasErrors { get; set; }
    public string? ErrorText { get; set; }
    public ImmutableHashSet<BreakpointAddressRange> AddressRanges { get; set; }
    /// <summary>
    /// Checkpoint number as returned by VICE for each address range.
    /// </summary>
    readonly Dictionary<uint, BreakpointAddressRange> checkpointNumbers = new();
    public BreakpointViewModel()
    {
        Bind = BreakpointNoBind.Empty;
        AddressRanges = ImmutableHashSet<BreakpointAddressRange>.Empty;        
    }
    public BreakpointViewModel(bool stopWhenHit, bool isEnabled, BreakpointMode mode, BreakpointBind bind,
        ImmutableHashSet<BreakpointAddressRange> addressRanges, string? condition)
    {
        StopWhenHit = stopWhenHit;
        IsEnabled = isEnabled;
        Mode = mode;
        Bind = bind;
        AddressRanges = addressRanges;
        Condition = condition;
    }
    public void ClearError()
    {
        HasErrors = false;
        ErrorText = null;
    }
    public void SetError(string errorText)
    {
        HasErrors = true;
        ErrorText = errorText;
    }
    public void ClearCheckpointNumbers()
    {
        checkpointNumbers.Clear();
        OnPropertyChanged(nameof(CheckpointNumbers));
    }
    public void AddCheckpointNumber(BreakpointAddressRange addressRange, uint checkpointNumber)
    {
        checkpointNumbers.Add(checkpointNumber, addressRange);
        OnPropertyChanged(nameof(CheckpointNumbers));
    }
    public void RemoveCheckpointNumber(uint checkpointNumber)
    {
        checkpointNumbers.Remove(checkpointNumber);
        OnPropertyChanged(nameof(CheckpointNumbers));
    }
    public IEnumerable<uint> CheckpointNumbers
    {
        get
        {
            foreach (var cn in checkpointNumbers.Keys)
            {
                yield return cn;
            }
        }
    }
    public BreakpointBindMode BindMode
    {
        get => Bind switch
        {
            BreakpointLineBind => BreakpointBindMode.Line,
            BreakpointLabelBind => BreakpointBindMode.Label,
            BreakpointGlobalVariableBind => BreakpointBindMode.GlobalVariable,
            BreakpointNoBind => BreakpointBindMode.None,
            _ => throw new ArgumentOutOfRangeException(),
        };
        set
        {
            if (value != BindMode)
            {
                Bind = value switch
                {
                    BreakpointBindMode.None => new BreakpointNoBind(0, 0),
                    BreakpointBindMode.Label => new BreakpointLabelBind(new PdbLabel(0, "none")),
                    BreakpointBindMode.Line => Bind, // does nothing
                    BreakpointBindMode.GlobalVariable => new BreakpointGlobalVariableBind(""),
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
        }
    }
    object ICloneable.Clone() => Clone();
    public BreakpointViewModel Clone()
    {
        return (BreakpointViewModel)MemberwiseClone();
    }
    /// <summary>
    /// Used to compare detail editing changes.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsChangedFrom(BreakpointViewModel other)
    {
        return !(StopWhenHit == other.StopWhenHit && IsEnabled == other.IsEnabled
            && Mode == other.Mode && Bind == other.Bind
            && string.Equals(Condition, other.Condition, StringComparison.Ordinal)
            && AddressRanges.SetEquals(other.AddressRanges));
    }
    internal bool AreCheckpointNumbersEqual(Dictionary<uint, BreakpointAddressRange> other)
    {
        if (checkpointNumbers.Count != other.Count)
        {
            return false;
        }
        foreach (var p in other)
        {
            if (!checkpointNumbers.TryGetValue(p.Key, out BreakpointAddressRange? value) || value != p.Value)
            {
                return false;
            }
        }
        return true;
    }
    /// <summary>
    /// Copies all properties from <paramref name="source"/>.
    /// </summary>
    /// <param name="source"></param>
    public void CopyFrom(BreakpointViewModel source)
    {
        IsCurrentlyHit = source.IsCurrentlyHit;
        StopWhenHit = source.StopWhenHit;
        IsEnabled = source.IsEnabled;
        HitCount = source.HitCount;
        IgnoreCount = source.IgnoreCount;
        Mode = source.Mode;
        Bind = source.Bind;
        AddressRanges = source.AddressRanges;
        Condition = source.Condition;
    }
}
public record BreakpointAddressRange(ushort Start, ushort End);
