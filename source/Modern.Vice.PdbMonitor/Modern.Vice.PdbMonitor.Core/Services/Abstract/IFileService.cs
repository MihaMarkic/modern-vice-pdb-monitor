using System.Collections.Immutable;
using System.IO;

namespace Modern.Vice.PdbMonitor.Core.Services.Abstract;
public interface IFileService
{
    ImmutableArray<string> ReadAllLines(string path);
    Stream OpenFileStream(string path, FileAccess access = FileAccess.Read);
}
