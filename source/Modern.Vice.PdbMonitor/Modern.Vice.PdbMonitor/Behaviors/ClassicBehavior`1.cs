using Avalonia;
using Avalonia.Xaml.Interactivity;

namespace Modern.Vice.PdbMonitor.Behaviors;

public abstract class ClassicBehavior<T> : Behavior<T>
          where T : AvaloniaObject
{
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is object)
        {
            Attached();
        }
    }
    protected virtual void Attached()
    { }

    protected virtual void Detached()
    { }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject is object)
        {
            Detached();
        }
    }
}
