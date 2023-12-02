using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;

public class PdbManager : IPdbManager
{
    readonly ILogger<PdbManager> logger;
    readonly Globals globals;
    public PdbManager(ILogger<PdbManager> logger, Globals globals)
    {
        this.logger = logger;
        this.globals = globals;
    }
    public PdbLine? FindLineUsingAddress(ushort address)
    {
        var lines = globals.Project?.DebugSymbols?.LinesWithAddress;
        if (lines.HasValue)
        {
            var matchingLine = BinarySearch(lines.Value, address);
            return matchingLine;
            //if (matchingLine is not null)
            //{
            //    var file = globals.Pdb!.Files[matchingLine.FileRelativePath];
            //    int matchingLineNumber = file.Lines.IndexOf(matchingLine);
            //}
        }
        return null;
    }
    public PdbLabel? FindLabel(string name)
    {
        PdbLabel? label = null;
        if (globals.Project?.DebugSymbols?.Labels.TryGetValue(name, out label) == true)
        {
            return label;
        }
        return null;
    }
    public PdbFile? FindFileOfLine(PdbLine line)
    {
        var file = globals.Project!.DebugSymbols!.GetFileOfLine(line);
        return file;
    }
    /// <summary>
    /// Applies binary search for line with given <paramref name="address"/>.
    /// </summary>
    /// <param name="lines">Code lines containing data.</param>
    /// <param name="address">Address to search for.</param>
    /// <returns></returns>
    public PdbLine? BinarySearch(ImmutableArray<PdbLine> lines, ushort address)
    {
        // TODO reimplement

        //int from = 0;
        //int to = lines.Length - 1;
        //PdbLine? line = null;
        //if (lines.Length == 0)
        //{
        //    return null;
        //}
        //// if there is no next line, then address has to fall between StartAddress and bytes length even though it might not be correct,
        //// or before the StartAddress of the next line
        //while (from < to)
        //{
        //    int middle = (from + to) / 2;
        //    line = lines[middle];

        //    // address has to be in an earlier line
        //    if (address < line.StartAddress)
        //    {
        //        to = Math.Max(middle - 1, from);
        //    }

        //    else if (line.IsAddressWithinLine(address))
        //    {
        //        return line;
        //    }
        //    else
        //    {
        //        from = Math.Min(middle + 1, to);
        //    }
        //}
        //line = lines[from];
        //if (line.IsAddressWithinLine(address))
        //{
        //    return line;
        //}

        //return null;
        var line = lines.Where(l => l.IsAddressWithinLine(address)).SingleOrDefault();
        return line;
    }
}
