using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public class TraceOutputViewModel: NotifiableObject
{
    readonly ILogger logger;
    readonly IViceBridge viceBridge;
    readonly Globals globals;
    readonly RegistersViewModel registersViewModel;
    readonly IDispatcher dispatcher;
    internal uint? CheckpointNumber { get; private set; }
    public string? Text { get; private set; }
    public RelayCommand ClearCommand { get; }
    public TraceOutputViewModel(ILogger<TraceOutputViewModel> logger, IViceBridge viceBridge,
        Globals globals, RegistersViewModel registersViewModel, IDispatcher dispatcher)
    {
        this.logger = logger;
        this.viceBridge = viceBridge;
        this.globals = globals;
        this.registersViewModel = registersViewModel;
        this.dispatcher = dispatcher;
        ClearCommand = new RelayCommand(Clear);
    }
    internal async Task CreateTraceCheckpointAsync(CancellationToken ct = default)
    {
        ushort? address = globals.Project?.DebugOutputAddress;
        if (address is not null)
        {
            var checkpointSetCommand = viceBridge.EnqueueCommand(
               new CheckpointSetCommand(address.Value, (ushort)(address.Value + 1), StopWhenHit: true,
               Enabled: true, CpuOperation.Exec, false),
               resumeOnStopped: true);
            var checkpointSetResponse = await checkpointSetCommand.Response
                .AwaitWithLogAndTimeoutAsync(dispatcher, logger, checkpointSetCommand, ct: ct);
            CheckpointNumber = checkpointSetResponse?.CheckpointNumber;
        }
    }
    void Clear()
    {
        Text = null;
        OnPropertyChanged(nameof(Text));
    }
    internal async Task ClearTraceCheckpointAsync(CancellationToken ct = default)
    {
        if (CheckpointNumber is not null)
        {
            var checkpointDeleteCommand = viceBridge.EnqueueCommand(new CheckpointDeleteCommand(CheckpointNumber.Value),
                resumeOnStopped: true);
            var checkpointDeleteResponse = await checkpointDeleteCommand.Response
                .AwaitWithLogAndTimeoutAsync(dispatcher, logger, checkpointDeleteCommand, ct: ct);
            if (checkpointDeleteCommand is null)
            {
                logger.LogWarning("Couldn't delete trace checkpoint");
            }
        }
    }

    //Stopwatch sw = Stopwatch.StartNew();
    //int count;
    bool isDelayingNotificaton;
    internal async Task LoadTraceLineAsync(CancellationToken ct)
    {
        var registers = registersViewModel.Current;
        if (registers.A.HasValue && registers.X.HasValue && registers.Y.HasValue)
        {
            ushort startAddress = (ushort)((registers.X << 8) + registers.A);
            ushort endAddress = (ushort)(startAddress + registers.Y);
            var command = viceBridge.EnqueueCommand(
                new MemoryGetCommand(0, startAddress, endAddress, MemSpace.MainMemory, 0));
            var response = await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command, ct: ct);
            using (var buffer = response?.Memory ?? throw new Exception("Failed to retrieve base address"))
            {
                string line = ASCIIEncoding.ASCII.GetString(buffer.Data, 0, (int)buffer.Size);
                Text = Text is null ? line : Text + Environment.NewLine + line;
            }
            viceBridge.EnqueueCommand(new ExitCommand(), resumeOnStopped: false);
        }
        else
        {
            logger.LogWarning("Got no registers on trace");
        }
    }
}
