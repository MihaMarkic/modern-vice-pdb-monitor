using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract;
public interface IMessageHistorySource: IMessagesHistory
{
    public IReadOnlyList<CommunicationData> History { get; }
    void Clear();
}
