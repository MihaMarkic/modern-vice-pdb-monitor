using System.Collections.ObjectModel;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class ErrorMessagesViewModel: NotifiableObject
    {
        public ObservableCollection<ErrorMessage> Messages { get; } = new ObservableCollection<ErrorMessage>();
        public Subscription messagesSubscription;
        public ErrorMessagesViewModel(IDispatcher dispatcher)
        {
            messagesSubscription = dispatcher.Subscribe<ErrorMessage>(OnErrorMessage);
        }
        void OnErrorMessage(object sender, ErrorMessage message)
        {
            Messages.Add(message);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                messagesSubscription.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
