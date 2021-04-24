using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace System
{
    public static class ServiceProviderExtension
    {
        public static SourceFileViewModel CreateSourceFileViewModel(this IServiceProvider serviceProvider, string path, ImmutableArray<Line> lines)
        {
            return ActivatorUtilities.CreateInstance<SourceFileViewModel>(serviceProvider, path, lines);
        }
    }
}
