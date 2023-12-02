using System.Diagnostics;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;
/// <inheritdoc/>
public class MessagesHistory : IMessageHistorySource
{
    readonly ObservableCollection<CommunicationData> history = new();
    readonly Stopwatch watch = new Stopwatch();
    readonly TaskFactory uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
    public IReadOnlyList<CommunicationData> History => history;
    public event EventHandler? Updated;
    /// <inheritdoc/>
    public void Start()
    {
        watch.Restart();
    }
    /// <inheritdoc/>
    public async ValueTask<int> AddCommandAsync(uint sequence, IViceCommand? command)
    {
        var data = new CommunicationData(sequence, command, default, watch.ElapsedTicks, default,
            ImmutableArray<ViceResponse>.Empty);
        int index = await uiFactory.StartNew(d =>
        {
            history.Add(data);
            return history.Count-1;
        }, data);
        Updated?.Invoke(this, EventArgs.Empty);
        return index;
    }
    /// <inheritdoc/>
    readonly record struct UpdateData(int Id, ViceResponse Response, ObservableCollection<CommunicationData> History);
    /// <inheritdoc/>
    public void UpdateWithLinkedResponse(int id, ViceResponse response)
    {
        _ = uiFactory.StartNew(d =>
        {
            var args = (UpdateData)d!;
            var data = args.History[args.Id];
            data = data with
            {
                LinkedResponses = data.LinkedResponses.Add(response)
            };
            args.History[args.Id] = data;
        }, new UpdateData(id, response, history));
    }
    /// <inheritdoc/>
    public void UpdateWithResponse(int id, ViceResponse response)
    {
        _ = uiFactory.StartNew(d =>
        {
            var args = (UpdateData)d!;
            var data = args.History[args.Id];
            data = data with
            {
                Response = args.Response,
                Elapsed = watch.ElapsedTicks - data.StartTime,
            };
            args.History[args.Id] = data;
        }, new UpdateData(id, response, history));
    }

    readonly record struct ResponseOnlyData(CommunicationData Data, ObservableCollection<CommunicationData> History);
    /// <inheritdoc/>
    public void AddsResponseOnly(ViceResponse response)
    {
        var data = new CommunicationData(null, null, response, watch.ElapsedTicks, null, ImmutableArray<ViceResponse>.Empty);
        _ = uiFactory.StartNew(d =>
        {
            var args = (ResponseOnlyData)d!;
            args.History.Add(args.Data);
        }, new ResponseOnlyData(data, history));
    }
    public void Clear()
    {
        _ = uiFactory.StartNew(d =>
        {
            var args = (ObservableCollection<CommunicationData>)d!;
            args.Clear();
        }, history);
    }
}
