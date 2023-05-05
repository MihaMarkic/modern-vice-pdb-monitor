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
               new CheckpointSetCommand(address.Value, (ushort)(address.Value + 2), StopWhenHit: true,
               Enabled: true, CpuOperation.Store, false));
            var checkpointSetResponse = await checkpointSetCommand.Response
                .AwaitWithLogAndTimeoutAsync(dispatcher, logger, checkpointSetCommand, ct: ct);
            CheckpointNumber = checkpointSetResponse?.CheckpointNumber;
        }
    }
    void Clear()
    {
        Text = null;
    }
    internal async Task ClearTraceCheckpointAsync(CancellationToken ct = default)
    {
        if (CheckpointNumber is not null)
        {
            var checkpointDeleteCommand = viceBridge.EnqueueCommand(new CheckpointDeleteCommand(CheckpointNumber.Value));
            var checkpointDeleteResponse = await checkpointDeleteCommand.Response
                .AwaitWithLogAndTimeoutAsync(dispatcher, logger, checkpointDeleteCommand, ct: ct);
            if (checkpointDeleteCommand is null)
            {
                logger.LogWarning("Couldn't delete trace checkpoint");
            }
        }
    }
    internal void LoadTraceChar()
    {
        char? c = (char?)registersViewModel.Current.A;
        if (c.HasValue)
        {
            if (Text is null)
            {
                Text = new string(c.Value, 1);
            }
            else
            {
                Text += c;
            }
        }
    }
}
