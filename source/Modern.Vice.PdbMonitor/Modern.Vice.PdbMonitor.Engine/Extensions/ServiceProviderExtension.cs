using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace System
{
    public static class ServiceProviderExtension
    {
        public static SourceFileViewModel CreateScopedSourceFileViewModel(this IServiceProvider serviceProvider, string path, ImmutableArray<LineViewModel> lines)
        {
            var contentScope = serviceProvider.CreateScope();
            var viewModel = ActivatorUtilities.CreateInstance<SourceFileViewModel>(serviceProvider, path, lines);
            viewModel.AssignScope(contentScope);
            return viewModel;
        }
        /// <summary>
        /// Creates an instance of <typeparamref name="T"/> within a new scope and stores scope in the newly created instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceProvider"></param>
        /// <returns>An instance of <typeparamref name="T"/>.</returns>
        public static T CreateScopedContent<T>(this IServiceProvider serviceProvider)
            where T : ScopedViewModel
{
            var contentScope = serviceProvider.CreateScope();
            T viewModel = contentScope.ServiceProvider.GetService<T>() ?? throw new Exception($"Failed creating {typeof(T).Name} ViewModel");
            viewModel.AssignScope(contentScope);
            return viewModel;
        }
    }
}
