using System;
using System.Windows.Input;

namespace Modern.Vice.PdbMonitor.Core.Common
{
    public class RelayCommand<T> : ICommand
    {
        private readonly Func<T?, bool>? canExecute;
        private readonly Action<T?> execute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<T?> execute) : this(execute, null)
        {
        }

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute)
        {
            this.execute = execute ?? throw new ArgumentNullException("execute");
            if (canExecute != null)
            {
                this.canExecute = new Func<T?, bool>(canExecute);
            }
        }

        public virtual bool CanExecute(object? parameter)
        {
            if (canExecute == null)
            {
                return true;
            }
            return canExecute((T?)parameter);
        }

        public virtual void Execute(object? parameter)
        {
            execute((T?)parameter);
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
