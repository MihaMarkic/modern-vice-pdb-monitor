using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Modern.Vice.PdbMonitor.Core.Common;
/// <summary>
/// Builder for <see cref="PdbAddressToLineMap"/> immutable map.
/// </summary>
public class PdbAddressToLineMapBuilder
{
    public SegmentsBuilder?[] Segments { get; } = new SegmentsBuilder[byte.MaxValue+1];
    public void Add(IList<AddressRange> addressRanges, PdbAddressToLineMap.SegmentItem item)
    {
        foreach (var r in addressRanges)
        {
            Add(r, item);
        }
    }
    public void Add(AddressRange addresses, PdbAddressToLineMap.SegmentItem item)
    {
        for (ushort a = addresses.StartAddress; a < addresses.EndAddress; a++)
        {
            Add(a, item);
        }
    }
    public void Add(ushort address, PdbAddressToLineMap.SegmentItem item)
    {
        byte high = (byte)(address >> 8);
        SegmentsBuilder? builder = Segments[high];
        if (builder is null)
        {
            builder = new SegmentsBuilder();
            Segments[high] = builder;
        }
        byte low = (byte)(address & 0xFF);
        if (builder.Addresses[low] is not null)
        {
            throw new Exception($"Failed assigning source line to address {address} because of duplicate");
        }
        builder.Addresses[low] = item;
    }

    public PdbAddressToLineMap ToImmutable()
    {
        return new PdbAddressToLineMap(
            Segments.Select(s => s is not null ? new PdbAddressToLineMap.Segment(s.ToImmutable()): null)
            .ToImmutableArray()
            );
    }

    public class SegmentsBuilder
    {
        public PdbAddressToLineMap.SegmentItem?[] Addresses { get; } = new PdbAddressToLineMap.SegmentItem?[byte.MaxValue+1];
        public ImmutableArray<PdbAddressToLineMap.SegmentItem?> ToImmutable()
        {
            return Addresses.ToImmutableArray();
        }
    }
}
