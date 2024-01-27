namespace Modern.Vice.PdbMonitor.Engine.Models.OpCodes;

public record Instruction(InstructionName Name, InstructionMode Mode);

public abstract record InstructionMode(byte OpCode, byte? FirstArgument, byte? SecondArgument, int Cycles, byte ModeLength)
{
    public int Length => ModeLength + 1;
}
public record ImmediateInstructionMode(byte OpCode, byte First, int Cycles): InstructionMode(OpCode, First, null, Cycles, 1)
{
    public override string ToString() => $"#{First:X2}";
}
public record ZeroPageInstructionMode(byte OpCode, byte First, int Cycles) : InstructionMode(OpCode, First, null, Cycles, 1)
{
    public override string ToString() => $"${First:X2}";
}
public record ZeroPageXInstructionMode(byte OpCode, byte First, int Cycles) : InstructionMode(OpCode, First, null, Cycles, 1)
{
    public override string ToString() => $"#{First:X2},X";
}
public record ZeroPageYInstructionMode(byte OpCode, byte First, int Cycles) : InstructionMode(OpCode, First, null, Cycles, 1)
{
    public override string ToString() => $"#{First:X2},Y";
}
public record AbsoluteInstructionMode(byte OpCode, byte First, byte Second, int Cycles) : InstructionMode(OpCode, First, Second, Cycles, 2)
{
    public override string ToString() => $"${Second:X2}{First:X2}";
}
public record AbsoluteXInstructionMode(byte OpCode, byte First, byte Second, int Cycles) : InstructionMode(OpCode, First, Second, Cycles, 2)
{
    public override string ToString() => $"${Second:X2}{First:X2},X";
}
public record AbsoluteYInstructionMode(byte OpCode, byte First, byte Second, int Cycles) : InstructionMode(OpCode, First, Second, Cycles, 2)
{
    public override string ToString() => $"${Second:X2}{First:X2},Y";
}
public record AccumulatorInstructionMode(byte OpCode, int Cycles) : InstructionMode(OpCode, null, null, Cycles, 0)
{
    public override string ToString() => "A";
}
public record RelativeInstructionMode(byte OpCode, sbyte First, int Cycles) : InstructionMode(OpCode, (byte)First, null, Cycles, 1)
{
    public override string ToString() => $"*+${First:X2}";
}
public record ImpliedInstructionMode(byte OpCode, int Cycles) : InstructionMode(OpCode, null, null, Cycles, 0)
{
    public override string ToString() => "";
}
public record IndirectInstructionMode(byte OpCode, byte First, byte Second, int Cycles) : InstructionMode(OpCode, First, Second, Cycles, 2)
{
    public override string ToString() => $"(${Second:X2}{First:X2})";
}
public record IndirectXInstructionMode(byte OpCode, byte First, int Cycles) : InstructionMode(OpCode, First, null, Cycles, 1)
{
    public override string ToString() => $"(${First:X2},X)";
}
public record IndirectYInstructionMode(byte OpCode, byte First, int Cycles) : InstructionMode(OpCode, First, null, Cycles, 1)
{
    public override string ToString() => $"(${First:X2}),Y";
}

