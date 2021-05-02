using System;
using System.Threading.Tasks;

namespace Modern.Vice.PdbMonitor.Core.Common
{
    public class RelayCommandAsync : ICommandEx
    {
        private readonly Func<bool>? canExecute;
        private readonly Func<Task> execute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommandAsync(Func<Task> execute) : this(execute, null)
        {
        }

        public RelayCommandAsync(Func<Task> execute, Func<bool>? canExecute)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public virtual bool CanExecute(object? parameter)
        {
            if (canExecute is null)
            {
                return true;
            }
            return canExecute();
        }

        public virtual void Execute(object? parameter) => _ = execute();
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
