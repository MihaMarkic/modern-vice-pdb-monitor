using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Core;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public abstract class ScopedViewModel: NotifiableObject
{
    public IServiceScope? Scope { get; private set; }
    internal void AssignScope(IServiceScope scope)
    {
        Scope = scope;
    }
}
