using System.Collections.Immutable;

namespace Modern.Vice.PdbMonitor.Core.Services.Abstract;
public interface IFileService
{
    ImmutableArray<string> ReadAllLines(string path);
}
