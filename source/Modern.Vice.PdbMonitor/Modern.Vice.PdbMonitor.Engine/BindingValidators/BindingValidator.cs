using Modern.Vice.PdbMonitor.Core;
using PropertyChanged;

namespace Modern.Vice.PdbMonitor.Engine.BindingValidators;
public abstract class BindingValidator : NotifiableObject, IBindingValidator
{
    public string SourcePropertyName { get; }
    public event EventHandler? HasErrorsChanged;
    public ImmutableArray<string> Errors { get; protected set; } = ImmutableArray<string>.Empty;
    public bool HasErrors => !Errors.IsDefaultOrEmpty;
    public BindingValidator(string sourcePropertyName)
    {
        SourcePropertyName = sourcePropertyName;
    }
    [SuppressPropertyChangedWarnings]
    protected virtual void OnHasErrorsChanged(EventArgs e) => HasErrorsChanged?.Invoke(this, e);
    public void Clear()
    {
        if (HasErrors)
        {
            Errors = ImmutableArray<string>.Empty;
            OnHasErrorsChanged(EventArgs.Empty);
        }
    }
    protected override void OnPropertyChanged(string name)
    {
        switch (name)
        {
            case nameof(HasErrors):
                OnHasErrorsChanged(EventArgs.Empty);
                break;
        }
    }
}
