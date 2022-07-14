using System;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Modern.Vice.PdbMonitor.Engine.Compilers.Acme.Grammar;

namespace Modern.Vice.PdbMonitor.Compilers.Acme.Test;

public abstract class Bootstrap
{
    public void Run<TContext>(string text, Func<AcmeParser, TContext> run)
        where TContext : ParserRuleContext
    {
        Run<AcmeBaseListener, TContext>(text, run);
    }
    public TListener Run<TListener, TContext>(string text, Func<AcmeParser, TContext> run)
    where TListener : AcmeBaseListener, new()
    where TContext : ParserRuleContext
    {
        var input = new AntlrInputStream(text);
        var lexer = new AcmeLexer(input);
        lexer.AddErrorListener(new SyntaxErrorListener());
        var tokens = new CommonTokenStream(lexer);
        var parser = new AcmeParser(tokens)
        {
            BuildParseTree = true
        };
        parser.AddErrorListener(new ErrorListener());
        var tree = run(parser);
        var listener = new TListener();
        ParseTreeWalker.Default.Walk(listener, tree);
        return listener;
    }
    public void Run<TContext>(IAcmeListener listener, string text, Func<AcmeParser, TContext> run)
        where TContext : ParserRuleContext
    {
        var input = new AntlrInputStream(text);
        var lexer = new AcmeLexer(input);
        lexer.AddErrorListener(new SyntaxErrorListener());
        var tokens = new CommonTokenStream(lexer);
        var parser = new AcmeParser(tokens)
        {
            BuildParseTree = true
        };
        parser.AddErrorListener(new ErrorListener());
        var tree = run(parser);
        ParseTreeWalker.Default.Walk(listener, tree);
    }
}

public class ErrorListener : BaseErrorListener
{
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        throw new Exception(msg, e);
    }
}

public class SyntaxErrorListener : IAntlrErrorListener<int>
{
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        throw new Exception(msg, e);
    }
}
