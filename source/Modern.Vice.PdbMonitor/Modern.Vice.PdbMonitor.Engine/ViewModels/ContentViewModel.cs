using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Core;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public abstract class ContentViewModel: NotifiableObject
    {
        protected IServiceScope Scope { get; private set; } = default!;
        internal void AssignScope(IServiceScope scope)
        {
            Scope = scope;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Scope.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
