using System;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public interface IDialogViewModel<TResult>
{
    Action<TResult>? Close { get; set; }
}
