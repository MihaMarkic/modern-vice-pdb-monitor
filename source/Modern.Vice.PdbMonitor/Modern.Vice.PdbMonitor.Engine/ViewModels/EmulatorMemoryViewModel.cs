using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public class EmulatorMemoryViewModel: NotifiableObject
{
    readonly ILogger<EmulatorMemoryViewModel> logger;
    readonly IViceBridge viceBridge;
    readonly IDispatcher dispatcher;
    byte[] previousSnapshot = new byte[ushort.MaxValue];
    byte[] currentSnapshot = new byte[ushort.MaxValue];
    public ReadOnlySpan<byte> Current => currentSnapshot.AsSpan();
    public ReadOnlySpan<byte> Previous => previousSnapshot.AsSpan();
    public EmulatorMemoryViewModel(ILogger<EmulatorMemoryViewModel> logger, IViceBridge viceBridge, IDispatcher dispatcher)
    {
        this.logger = logger;
        this.viceBridge = viceBridge;
        this.dispatcher = dispatcher;
    }

    public async Task GetSnapshotAsync(CancellationToken ct)
    {
        var command = viceBridge.EnqueueCommand(
        new MemoryGetCommand(0, 0, ushort.MaxValue-1, MemSpace.MainMemory, 0),
            true);
        var response = await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command, ct: ct);
        using (var buffer = response?.Memory ?? throw new Exception("Failed to retrieve base VICE memory"))
        {
            (currentSnapshot, previousSnapshot) = (previousSnapshot, currentSnapshot);
            Buffer.BlockCopy(buffer.Data, 0, currentSnapshot, 0, currentSnapshot.Length);
            OnPropertyChanged(nameof(Current));
            OnPropertyChanged(nameof(Previous));
        }
    }

    public void UpdateMemory(ushort start, ReadOnlySpan<byte> memory)
    {
        var target = currentSnapshot.AsSpan().Slice(start, memory.Length);
        memory.CopyTo(target);
        OnPropertyChanged(nameof(Current));
    }

    public ushort GetShortAt(ushort address)
    {
        if (address == ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(address));
        }
        return BitConverter.ToUInt16(currentSnapshot, address);
    }

    public ReadOnlySpan<byte> GetSpan(ushort start, ushort end)
    {
        if (start == ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }
        if (end == ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(end));
        }
        return currentSnapshot.AsSpan()[start..end];
    }
}
