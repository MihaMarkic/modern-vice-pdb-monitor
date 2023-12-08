using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Righthand.MessageBus;
internal static class DispatcherExtension
{
    /// <summary>
    /// Dispatches <see cref="ShowModalDialogMessage{TViewModel, TResult}"/> with proper signature (superclass)
    /// so the subscriber can catch all of them.
    /// </summary>
    /// <typeparam name="TViewModel"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="dispatcher"></param>
    /// <param name="message"></param>
    internal static void DispatchShowModalDialog<TViewModel, TResult>(this IDispatcher dispatcher, 
        ShowModalDialogMessage<TViewModel, TResult> message)
        where TViewModel : NotifiableObject, IDialogViewModel<TResult>
    {
        dispatcher.Dispatch<ShowModalDialogMessageCore>(message);
    }
}
