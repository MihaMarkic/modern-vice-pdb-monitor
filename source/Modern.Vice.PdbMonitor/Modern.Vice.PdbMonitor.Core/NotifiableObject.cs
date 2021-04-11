using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Modern.Vice.PdbMonitor.Core
{
    /// <summary>
    /// Base class that implements <see cref="INotifyPropertyChanged"/> 
    /// </summary>
    public abstract class NotifiableObject : DisposableObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName]string name = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
