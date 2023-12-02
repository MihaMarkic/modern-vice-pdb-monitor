namespace Modern.Vice.PdbMonitor.Engine.BindingValidators;

public interface IBindingValidator
{
    string SourcePropertyName { get; }
    event EventHandler? HasErrorsChanged;
    ImmutableArray<string> Errors { get; }
    bool HasErrors { get; }
}
