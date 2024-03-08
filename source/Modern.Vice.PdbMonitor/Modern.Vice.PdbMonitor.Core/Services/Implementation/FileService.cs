using System.Collections.Immutable;
using System.IO;
using Modern.Vice.PdbMonitor.Core.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Core.Services.Implementation;
public class FileService : IFileService
{
    public ImmutableArray<string> ReadAllLines(string path) => File.ReadAllLines(path).ToImmutableArray();
    public Stream OpenFileStream(string path, FileAccess access = FileAccess.Read) => File.Open(path, FileMode.Open, access);
}
