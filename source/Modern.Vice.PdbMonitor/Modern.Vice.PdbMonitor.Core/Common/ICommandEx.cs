using System.Windows.Input;

namespace Modern.Vice.PdbMonitor.Core.Common;

public interface ICommandEx: ICommand
{
    void RaiseCanExecuteChanged();
}
