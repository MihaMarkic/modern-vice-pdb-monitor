namespace Modern.Vice.PdbMonitor.Engine.Models
{
    public abstract record Registers;
    public record Registers6510(ushort? PC, byte? A, byte? X, byte? Y, byte? SP, byte? Zero, byte? One, byte? Flags, ushort Lin, ushort Cyc,
        byte? R3,
        byte? R4,
        byte? R5,
        byte? R6,
        byte? R7,
        byte? R8,
        byte? R9,
        byte? R10,
        byte? R11,
        byte? R12,
        byte? R13,
        byte? R14,
        byte? R15,
        byte? Acm, byte? Yxm)
    {
        public readonly static Registers6510 Empty = new(default, default, default, default, 
            default, default, default, default, default, default, default, default,
            default, default, default, default, default, default, default, default, 
            default, default, default, default, default);
    }

    public enum Register6510
    {
        A,
        X,
        Y,
        PC,
        SP,
        Zero,
        One,
        Flags,
        Lin,
        Cyc,
        R3,
        R4,
        R5,
        R6,
        R7,
        R8,
        R9,
        R10,
        R11,
        R12,
        R13,
        R14,
        R15,
        Acm,
        Yxm
    }
}
