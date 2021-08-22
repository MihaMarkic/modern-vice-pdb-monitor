//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.9.2
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from Acme.g4 by ANTLR 4.9.2

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

namespace Modern.Vice.PdbMonitor.Engine.Compilers.Acme.Grammar {
using Antlr4.Runtime.Misc;
using IParseTreeListener = Antlr4.Runtime.Tree.IParseTreeListener;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete listener for a parse tree produced by
/// <see cref="AcmeParser"/>.
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.9.2")]
[System.CLSCompliant(false)]
public interface IAcmeListener : IParseTreeListener {
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.pseudoOps"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterPseudoOps([NotNull] AcmeParser.PseudoOpsContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.pseudoOps"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitPseudoOps([NotNull] AcmeParser.PseudoOpsContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.expressionPseudoOps"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterExpressionPseudoOps([NotNull] AcmeParser.ExpressionPseudoOpsContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.expressionPseudoOps"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitExpressionPseudoOps([NotNull] AcmeParser.ExpressionPseudoOpsContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.hexByteValues"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterHexByteValues([NotNull] AcmeParser.HexByteValuesContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.hexByteValues"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitHexByteValues([NotNull] AcmeParser.HexByteValuesContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.fillValues"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterFillValues([NotNull] AcmeParser.FillValuesContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.fillValues"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitFillValues([NotNull] AcmeParser.FillValuesContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.skipValues"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterSkipValues([NotNull] AcmeParser.SkipValuesContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.skipValues"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitSkipValues([NotNull] AcmeParser.SkipValuesContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.alignValues"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterAlignValues([NotNull] AcmeParser.AlignValuesContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.alignValues"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitAlignValues([NotNull] AcmeParser.AlignValuesContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.convtab"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterConvtab([NotNull] AcmeParser.ConvtabContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.convtab"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitConvtab([NotNull] AcmeParser.ConvtabContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.stringValues"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterStringValues([NotNull] AcmeParser.StringValuesContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.stringValues"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitStringValues([NotNull] AcmeParser.StringValuesContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.scrxor"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterScrxor([NotNull] AcmeParser.ScrxorContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.scrxor"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitScrxor([NotNull] AcmeParser.ScrxorContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.to"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterTo([NotNull] AcmeParser.ToContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.to"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitTo([NotNull] AcmeParser.ToContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.source"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterSource([NotNull] AcmeParser.SourceContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.source"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitSource([NotNull] AcmeParser.SourceContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.binary"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterBinary([NotNull] AcmeParser.BinaryContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.binary"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitBinary([NotNull] AcmeParser.BinaryContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.zone"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterZone([NotNull] AcmeParser.ZoneContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.zone"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitZone([NotNull] AcmeParser.ZoneContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.symbollist"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterSymbollist([NotNull] AcmeParser.SymbollistContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.symbollist"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitSymbollist([NotNull] AcmeParser.SymbollistContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.flowOps"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterFlowOps([NotNull] AcmeParser.FlowOpsContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.flowOps"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitFlowOps([NotNull] AcmeParser.FlowOpsContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.ifFlow"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterIfFlow([NotNull] AcmeParser.IfFlowContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.ifFlow"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitIfFlow([NotNull] AcmeParser.IfFlowContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.ifDefFlow"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterIfDefFlow([NotNull] AcmeParser.IfDefFlowContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.ifDefFlow"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitIfDefFlow([NotNull] AcmeParser.IfDefFlowContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.forFlow"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterForFlow([NotNull] AcmeParser.ForFlowContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.forFlow"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitForFlow([NotNull] AcmeParser.ForFlowContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.set"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterSet([NotNull] AcmeParser.SetContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.set"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitSet([NotNull] AcmeParser.SetContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.doFlow"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterDoFlow([NotNull] AcmeParser.DoFlowContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.doFlow"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitDoFlow([NotNull] AcmeParser.DoFlowContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.whileFlow"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterWhileFlow([NotNull] AcmeParser.WhileFlowContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.whileFlow"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitWhileFlow([NotNull] AcmeParser.WhileFlowContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.endOfFile"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterEndOfFile([NotNull] AcmeParser.EndOfFileContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.endOfFile"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitEndOfFile([NotNull] AcmeParser.EndOfFileContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.reportError"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterReportError([NotNull] AcmeParser.ReportErrorContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.reportError"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitReportError([NotNull] AcmeParser.ReportErrorContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.errorLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterErrorLevel([NotNull] AcmeParser.ErrorLevelContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.errorLevel"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitErrorLevel([NotNull] AcmeParser.ErrorLevelContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.macro"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterMacro([NotNull] AcmeParser.MacroContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.macro"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitMacro([NotNull] AcmeParser.MacroContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.callMarco"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterCallMarco([NotNull] AcmeParser.CallMarcoContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.callMarco"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitCallMarco([NotNull] AcmeParser.CallMarcoContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.callMacroArgument"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterCallMacroArgument([NotNull] AcmeParser.CallMacroArgumentContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.callMacroArgument"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitCallMacroArgument([NotNull] AcmeParser.CallMacroArgumentContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.setProgramCounter"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterSetProgramCounter([NotNull] AcmeParser.SetProgramCounterContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.setProgramCounter"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitSetProgramCounter([NotNull] AcmeParser.SetProgramCounterContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.initMem"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterInitMem([NotNull] AcmeParser.InitMemContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.initMem"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitInitMem([NotNull] AcmeParser.InitMemContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.xor"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterXor([NotNull] AcmeParser.XorContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.xor"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitXor([NotNull] AcmeParser.XorContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.pseudoPc"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterPseudoPc([NotNull] AcmeParser.PseudoPcContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.pseudoPc"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitPseudoPc([NotNull] AcmeParser.PseudoPcContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.expressionPseudoCodes"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterExpressionPseudoCodes([NotNull] AcmeParser.ExpressionPseudoCodesContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.expressionPseudoCodes"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitExpressionPseudoCodes([NotNull] AcmeParser.ExpressionPseudoCodesContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.block"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterBlock([NotNull] AcmeParser.BlockContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.block"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitBlock([NotNull] AcmeParser.BlockContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterStatement([NotNull] AcmeParser.StatementContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitStatement([NotNull] AcmeParser.StatementContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.statements"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterStatements([NotNull] AcmeParser.StatementsContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.statements"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitStatements([NotNull] AcmeParser.StatementsContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.filename"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterFilename([NotNull] AcmeParser.FilenameContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.filename"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitFilename([NotNull] AcmeParser.FilenameContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.condition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterCondition([NotNull] AcmeParser.ConditionContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.condition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitCondition([NotNull] AcmeParser.ConditionContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.comment"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterComment([NotNull] AcmeParser.CommentContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.comment"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitComment([NotNull] AcmeParser.CommentContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterExpression([NotNull] AcmeParser.ExpressionContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitExpression([NotNull] AcmeParser.ExpressionContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.number"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterNumber([NotNull] AcmeParser.NumberContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.number"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitNumber([NotNull] AcmeParser.NumberContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.decNumber"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterDecNumber([NotNull] AcmeParser.DecNumberContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.decNumber"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitDecNumber([NotNull] AcmeParser.DecNumberContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.hexNumber"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterHexNumber([NotNull] AcmeParser.HexNumberContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.hexNumber"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitHexNumber([NotNull] AcmeParser.HexNumberContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.binNumber"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterBinNumber([NotNull] AcmeParser.BinNumberContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.binNumber"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitBinNumber([NotNull] AcmeParser.BinNumberContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.logicalop"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterLogicalop([NotNull] AcmeParser.LogicalopContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.logicalop"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitLogicalop([NotNull] AcmeParser.LogicalopContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.symbol"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterSymbol([NotNull] AcmeParser.SymbolContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.symbol"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitSymbol([NotNull] AcmeParser.SymbolContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.binaryop"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterBinaryop([NotNull] AcmeParser.BinaryopContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.binaryop"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitBinaryop([NotNull] AcmeParser.BinaryopContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="AcmeParser.opcode"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterOpcode([NotNull] AcmeParser.OpcodeContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="AcmeParser.opcode"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitOpcode([NotNull] AcmeParser.OpcodeContext context);
}
} // namespace Modern.Vice.PdbMonitor.Engine.Compilers.Acme.Grammar
