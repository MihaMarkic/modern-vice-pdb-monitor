using Modern.Vice.PdbMonitor.Engine.Models.OpCodes;

namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract;
public interface IDisassembler
{
    ImmutableArray<Instruction> Disassemble(ReadOnlySpan<byte> data);
    Instruction? DisassembleInstruction(ReadOnlySpan<byte> data);
}
