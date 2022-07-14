# ANTLR C# code generation

1. Follow ANTLR v4 installation instructions at [Getting Started with ANTLR v4](https://github.com/antlr/antlr4/blob/master/doc/getting-started.md#getting-started-with-antlr-v4)
1. Run generator with `java org.antlr.v4.Tool -Dlanguage=CSharp -package Modern.Vice.PdbMonitor.Engine.Compilers.Acme.Grammar Acme.g4` from within `Modern.Vice.PdbMonitor.Compilers.Acme\Grammar`. Similar for other supported compilers.