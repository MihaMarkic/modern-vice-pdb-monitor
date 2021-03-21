using System.Threading.Tasks;

namespace ModernVicePdbMonitor.Engine
{
    public abstract record ViceCommand(ViceCommandMode Mode);
    public record ViceBinnaryCommand : ViceCommand
    {
        public byte[] Content { get; init; }
        public Task<ManagedBuffer?> Result => tcs.Task;
        readonly TaskCompletionSource<ManagedBuffer?> tcs;
        public ViceBinnaryCommand(int start, int end) : base(ViceCommandMode.ReturnResults)
        {
            tcs = new TaskCompletionSource<ManagedBuffer?>();
            Content = new byte[5]
            {
                0x1, // mem dump
                (byte)(start & 255),
                (byte)((start >> 8) & 255),
                (byte)(end & 255),
                (byte)((end >> 8) & 255),
            };
        }
        public void SetResult(ManagedBuffer? buffer)
        {
            tcs.SetResult(buffer);
        }
    }
    public abstract record ViceTextCommand<TResult> : ViceCommand
    {
        public string Content { get; init; }
        public Task<TResult?> Result => tcs.Task;
        readonly TaskCompletionSource<TResult?> tcs;
        public ViceTextCommand(string content, ViceCommandMode mode): base(mode)
        {
            tcs = new TaskCompletionSource<TResult?>();
            Content = content;
        }
        public abstract void RespondWithResult(string text);
        public void RespondWithoutResult()
        {
            tcs.SetResult(default);
        }
    }
    public interface IViceTextCommand
    {
        string Content { get; }
        void RespondWithResult(string text);
        ViceCommandMode Mode { get; }
    }

    public enum ViceCommandMode
    {
        ThrowAwayResults,
        ReturnResults,
        CommandOnly,
    }
}
