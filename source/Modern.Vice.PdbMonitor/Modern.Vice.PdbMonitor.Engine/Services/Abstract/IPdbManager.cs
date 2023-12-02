using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract;

public interface IPdbManager
{
    /// <summary>
    /// Applies binary search for line with given <paramref name="address"/>.
    /// </summary>
    /// <param name="lines">Code lines containing data.</param>
    /// <param name="address">Address to search for.</param>
    /// <returns></returns>
    PdbLine? BinarySearch(ImmutableArray<PdbLine> lines, ushort address);
    PdbLine? FindLineUsingAddress(ushort address);
    PdbFile? FindFileOfLine(PdbLine line);
    PdbLabel? FindLabel(string name);
}
