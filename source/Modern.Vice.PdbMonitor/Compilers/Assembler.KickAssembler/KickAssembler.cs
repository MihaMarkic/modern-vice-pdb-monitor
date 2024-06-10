using System.Collections.Immutable;
using Modern.Vice.PdbMonitor.Core.Common.Compiler;
using Modern.Vice.PdbMonitor.Core.Services.Abstract;

namespace Assembler.KickAssembler;

public class KickAssembler: ICompiler
{
    public SourceLanguage Language => SourceLanguage.Custom;
    /// <summary>
    /// Parses source files
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    /// <remarks>Not implemented for now.</remarks>
    public ImmutableDictionary<int, ImmutableArray<SyntaxElement>> GetSyntaxElements(string code)
    {
        return ImmutableDictionary<int, ImmutableArray<SyntaxElement>>.Empty;
    }
}
