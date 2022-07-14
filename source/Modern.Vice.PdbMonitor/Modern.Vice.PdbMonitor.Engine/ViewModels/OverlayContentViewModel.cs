using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public abstract class OverlayContentViewModel : ScopedViewModel
{
    protected readonly IDispatcher dispatcher;
    public RelayCommand CloseCommand { get; }
    public OverlayContentViewModel(IDispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
        CloseCommand = new(() => dispatcher.Dispatch(new CloseOverlayMessage()));
    }
}
