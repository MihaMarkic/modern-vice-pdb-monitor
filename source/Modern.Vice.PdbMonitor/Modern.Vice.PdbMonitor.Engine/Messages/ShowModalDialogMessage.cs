using System;
using System.Threading.Tasks;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Engine.Messages
{
    public abstract record ShowModalDialogMessageCore
    {
        public string Caption { get; }
        public DialogButton Buttons { get; }
        public event EventHandler? Close;
        public NotifiableObject ViewModel { get; }
        public ShowModalDialogMessageCore(string caption, DialogButton buttons, NotifiableObject viewModel)
        {
            Caption = caption;
            Buttons = buttons;
            ViewModel = viewModel;
        }
        protected void OnClose(EventArgs e)
        {
            Close?.Invoke(this, e);
        }
    }
    public record ShowModalDialogMessage<TViewModel, TResult> : ShowModalDialogMessageCore
        where TViewModel: NotifiableObject, IDialogViewModel<TResult>
    {
        readonly TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
        public Task<TResult> Result => tcs.Task;
        public new TViewModel ViewModel => (TViewModel)base.ViewModel;
        public ShowModalDialogMessage(string caption, DialogButton buttons, TViewModel viewModel) :
            base(caption, buttons, viewModel)
        {
            ViewModel.Close = r =>
            {
                OnClose(EventArgs.Empty);
                tcs.SetResult(r);
            };
        }
    }

    public record SimpleDialogResult
    {
        public DialogResultCode Code { get; init; }
        public SimpleDialogResult(DialogResultCode code)
        {
            Code = code;
        }
    }
}
