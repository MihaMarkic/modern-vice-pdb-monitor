using System;
using System.Diagnostics;
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
    readonly object sync = new object();
    internal uint? CheckpointNumber { get; private set; }
    string? text;
    public string? Text => text;
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
        text = null;
        OnPropertyChanged(nameof(Text));
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

    //Stopwatch sw = Stopwatch.StartNew();
    //int count;
    bool isDelayingNotificaton;
    internal void LoadTraceChar()
    {
        char? c = (char?)registersViewModel.Current.A;
        if (c.HasValue)
        {
            //count++;
            //if (count % 1000 == 0)
            //{
            //    double avg = sw.ElapsedMilliseconds / (double)count;
            //    Debug.WriteLine($"Avg: {avg:#,##0.00}");
            //}
            // both 13 and 10 are valid line separators, but not together
            bool isNewLine = c == 13 || c == 10;
            if (text is null)
            {
                if (isNewLine)
                {
                    text = Environment.NewLine;
                }
                else
                {
                    text = new string(c.Value, 1);
                }
            }
            else
            {
                text += isNewLine ? Environment.NewLine : c;
            }
            if (!isDelayingNotificaton)
            {
                _ = NotifyTextChangedAsync();
            }
        }
    }
    /// <summary>
    /// Group notifications into a 10ms window to avoid overkill in UI refresh.
    /// </summary>
    /// <returns></returns>
    async Task NotifyTextChangedAsync()
    {
        isDelayingNotificaton = true;
        try
        {
            await Task.Delay(10);
            OnPropertyChanged(nameof(Text));
        }
        finally
        {
            isDelayingNotificaton = false;
        }
    }
}
