using System;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation
{
    public class AcmePdbManager : IAcmePdbManager
    {
        readonly ILogger<AcmePdbManager> logger;
        readonly Globals globals;
        public AcmePdbManager(ILogger<AcmePdbManager> logger, Globals globals)
        {
            this.logger = logger;
            this.globals = globals;
        }
        public AcmeLine? FindLineUsingAddress(ushort address)
        {
            var lines = globals.Pdb?.LinesWithAddress;
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
        public AcmeLabel? FindLabel(string name)
        {
            AcmeLabel? label = null;
            if (globals.Pdb?.Labels.TryGetValue(name, out label) == true)
            {
                return label;
            }
            return null;
        }
        public AcmeFile? FindFileOfLine(AcmeLine line)
        {
            var file = globals.Pdb!.Files[line.FileRelativePath];
            return file;
        }
        /// <summary>
        /// Applies binary search for line with given <paramref name="address"/>.
        /// </summary>
        /// <param name="lines">Code lines containing data.</param>
        /// <param name="address">Address to search for.</param>
        /// <returns></returns>
        public AcmeLine? BinarySearch(ImmutableArray<AcmeLine> lines, ushort address)
        {
            int from = 0;
            int to = lines.Length - 1;
            AcmeLine? line = null;
            if (lines.Length == 0)
            {
                return null;
            }
            // if there is no next line, then address has to fall between StartAddress and bytes length even though it might not be correct,
            // or before the StartAddress of the next line
            while (from < to)
            {
                int middle = (from + to) / 2;
                line = lines[middle];

                // address has to be in an earlier line
                if (address < line.StartAddress)
                {
                    to = Math.Max(middle - 1, from);
                }

                else if (line.IsAddressWithinLine(address))
                {
                    return line;
                }
                else
                {
                    from = Math.Min(middle + 1, to);
                }
            }
            line = lines[from];
            if (line.IsAddressWithinLine(address))
            {
                return line;
            }

            return null;
        }
    }
}
