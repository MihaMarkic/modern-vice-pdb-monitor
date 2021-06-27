using Modern.Vice.PdbMonitor.Engine.Models;
using System;
using System.Collections.Immutable;

namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract
{
    public interface IAcmePdbManager
    {
        /// <summary>
        /// Applies binary search for line with given <paramref name="address"/>.
        /// </summary>
        /// <param name="lines">Code lines containing data.</param>
        /// <param name="address">Address to search for.</param>
        /// <returns></returns>
        AcmeLine? BinarySearch(ImmutableArray<AcmeLine> lines, ushort address);
        AcmeLine? FindLineUsingAddress(ushort address);
        AcmeFile? FindFileOfLine(AcmeLine line);
        AcmeLabel? FindLabel(string name);
    }
}
