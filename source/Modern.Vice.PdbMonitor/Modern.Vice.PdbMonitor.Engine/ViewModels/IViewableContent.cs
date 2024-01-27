using Microsoft.Extensions.DependencyInjection;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
/// <summary>
/// Implemented by view models that can be displayed to user
/// </summary>
public interface IViewableContent
{
    string Caption { get; }
    IServiceScope? Scope { get; }
    void ClearExecutionRow();
}
