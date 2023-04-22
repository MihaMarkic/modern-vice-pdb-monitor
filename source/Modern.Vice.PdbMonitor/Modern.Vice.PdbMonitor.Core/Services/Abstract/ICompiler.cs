using System.Collections.Immutable;
using Modern.Vice.PdbMonitor.Core.Common.Compiler;

namespace Modern.Vice.PdbMonitor.Core.Services.Abstract;

public interface ICompiler
{
    SourceLanguage Language { get; }
    ImmutableDictionary<int, ImmutableArray<SyntaxElement>> GetSyntaxElements(string code);
}
