namespace ModernVicePdbMonitor.Engine
{
    public abstract record ViceCommand(ViceCommandMode Mode);
    public record ViceBinnaryCommand : ViceCommand
    {
        public byte[] Command { get; init; }

        public ViceBinnaryCommand(int start, int end) : base(ViceCommandMode.ReturnResults)
        {
            Command = new byte[5]
            {
                0x1, // mem dump
                (byte)(start & 255),
                (byte)((start >> 8) & 255),
                (byte)(end & 255),
                (byte)((end >> 8) & 255),
            };
        }
    }
    public record ViceTextCommand(string Command, ViceCommandMode Mode) : ViceCommand(Mode);

    public enum ViceCommandMode
    {
        ThrowAwayResults = 0,
        ReturnResults = 1,
        ThenExit = 2,
        CommandOnly = 3,
        FireCallback = 4,
    }
}
