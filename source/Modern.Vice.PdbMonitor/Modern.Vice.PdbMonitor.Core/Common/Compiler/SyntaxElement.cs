using System;
using System.Diagnostics;

namespace Modern.Vice.PdbMonitor.Core.Common.Compiler;

[DebuggerDisplay("{Text,nq}")]
public record SyntaxElement(string Content, SyntaxElementType ElementType, int Line, int Start, int End)
{
    public ReadOnlySpan<char> Text => Content.AsSpan().Slice(Start, End - Start + 1);
}

public enum SyntaxElementType
{
    String,
    Comment
}
