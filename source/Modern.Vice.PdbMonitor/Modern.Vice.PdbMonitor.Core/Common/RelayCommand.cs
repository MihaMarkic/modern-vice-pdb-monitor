using System;

namespace Modern.Vice.PdbMonitor.Core.Common;

public class RelayCommand : ICommandEx
{
    private readonly Func<bool>? canExecute;
    private readonly Action execute;

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action execute) : this(execute, null)
    {
    }

    public RelayCommand(Action execute, Func<bool>? canExecute)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        if (canExecute != null)
        {
            this.canExecute = new Func<bool>(canExecute);
        }
    }

    public virtual bool CanExecute(object? parameter)
    {
        if (canExecute == null)
        {
            return true;
        }
        return canExecute();
    }

    public virtual void Execute(object? parameter) => execute();
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
