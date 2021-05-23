//using System;
//using System.Threading.Tasks;

//namespace Modern.Vice.PdbMonitor.Core.Common
//{
//    public class RelayCommandGenericAsync<T> : ICommandEx
//    {
//        private readonly Func<T, bool>? canExecute;
//        private readonly Func<T, Task> execute;

//        public event EventHandler? CanExecuteChanged;

//        public RelayCommandGenericAsync(Func<T, Task> execute) : this(execute, null)
//        {
//        }

//        public RelayCommandGenericAsync(Func<T, Task> execute, Func<T, bool>? canExecute)
//        {
//            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
//            this.canExecute = canExecute;
//        }

//        public virtual bool CanExecute(object? parameter)
//        {
//            if (canExecute is null)
//            {
//                return true;
//            }
//            return canExecute((T)parameter);
//        }

//        public virtual void Execute(object? parameter) => _ = execute((T)(parameter ?? throw new ArgumentNullException(nameof(parameter)));
//        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
//    }
//}
