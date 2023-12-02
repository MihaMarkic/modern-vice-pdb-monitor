using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace System;

public static class ServiceProviderExtension
{
    public static SourceFileViewModel CreateScopedSourceFileViewModel(this IServiceProvider serviceProvider, PdbFile file, ImmutableArray<LineViewModel> lines)
    {
        var contentScope = serviceProvider.CreateScope();
        var viewModel = ActivatorUtilities.CreateInstance<SourceFileViewModel>(serviceProvider, file, lines);
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
    public static BreakpointDetailViewModel CreateScopedBreakpointDetailViewModel(this IServiceScope serviceScope,
        BreakpointViewModel breakpointViewModel, BreakpointDetailDialogMode mode)
    {
        var viewModel = ActivatorUtilities.CreateInstance<BreakpointDetailViewModel>(serviceScope.ServiceProvider,
            serviceScope.ServiceProvider.GetRequiredService<ILogger<BreakpointDetailViewModel>>(),
            serviceScope.ServiceProvider.GetRequiredService<BreakpointsViewModel>(),
            breakpointViewModel, mode);
        return viewModel;
    }
}
