using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public class MessagesHistoryViewModel: NotifiableObject
{
    readonly ILogger<MessagesHistoryViewModel> logger;
    readonly IMessageHistorySource? messagesHistorySource;
    readonly RegistersMapping registersMapping;

    public RelayCommand ClearCommand { get; }
    public IReadOnlyList<CommunicationData> History { get; }
    public bool IsAvailable => messagesHistorySource is not null;
    public byte? PCRegisterId { get; private set; }

    public MessagesHistoryViewModel(ILogger<MessagesHistoryViewModel> logger, IMessagesHistory messagesHistory,
        RegistersMapping registersMapping)
    {
        this.logger = logger;
        this.registersMapping = registersMapping;
        ClearCommand = new RelayCommand(Clear, () => this.messagesHistorySource is not null);
        if (messagesHistory is IMessageHistorySource messagesHistorySource)
        {
            this.messagesHistorySource = messagesHistorySource;
            History = messagesHistorySource.History;
        }
        else
        {
            this.messagesHistorySource = null;
            History = ImmutableArray<CommunicationData>.Empty;
        }
    }
    public void Start()
    {
        PCRegisterId = registersMapping.GetRegisterId(Models.Register6510.PC);
        messagesHistorySource?.Start();
    }
    public void Clear()
    {
        messagesHistorySource?.Clear();
    }
}
