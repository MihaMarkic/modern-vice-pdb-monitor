using System.Collections.Generic;
using System.Collections.Immutable;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Modern.Vice.PdbMonitor.Core.Common.Compiler;
using Modern.Vice.PdbMonitor.Core.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.Compilers.Acme.Grammar;

namespace Modern.Vice.PdbMonitor.Compilers.Acme
{
    public class AcmeCompiler : ICompiler
    {
        readonly ImmutableDictionary<int, SyntaxElementType> tokenTypeMap = ImmutableDictionary<int, SyntaxElementType>.Empty
            .Add(AcmeLexer.STRING, SyntaxElementType.String)
            .Add(AcmeLexer.COMMENT, SyntaxElementType.Comment);
        public ImmutableDictionary<int, ImmutableArray<SyntaxElement>> GetSyntaxElements(string code)
        {
            var input = new AntlrInputStream(code);
            var lexer = new AcmeLexer(input);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new AcmeParser(tokenStream)
            {
                BuildParseTree = true
            };
            var tree = parser.prog();
            var listener = new AcmeBaseListener();
            ParseTreeWalker.Default.Walk(listener, tree);

            var tokens = tokenStream.GetTokens().ToImmutableArray();
            var result = new Dictionary<int, ImmutableArray<SyntaxElement>>();
            foreach (var token in tokens)
            {
                if (tokenTypeMap.TryGetValue(token.Type, out var syntaxElementType))
                {
                    if (!result.TryGetValue(token.Line, out var elements))
                    {
                        elements = ImmutableArray<SyntaxElement>.Empty;
                    }
                    elements = elements.Add(new SyntaxElement(code, syntaxElementType, token.Line, token.StartIndex, token.StopIndex));
                    result[token.Line] = elements;
                }
            }
            return result.ToImmutableDictionary();
        }
    }
}
