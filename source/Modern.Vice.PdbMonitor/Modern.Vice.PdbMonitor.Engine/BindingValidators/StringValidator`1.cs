using PropertyChanged;

namespace Modern.Vice.PdbMonitor.Engine.BindingValidators;

/// <summary>
/// Provides validation of bound value before it is being converted.
/// ViewModel should route a string value to <see cref="TextValue"/> property and track <see cref="HasErrorsChanged"/> event.
/// </summary>
/// <typeparam name="TSource">Source type for UI string value.</typeparam>
public abstract class StringValidator<TSource> : BindingValidator, IBindingValidator
{
    readonly Action<TSource> assignToSource;
    public string? TextValue { get; protected set; }
    public abstract string? ConvertTo(TSource source);
    public abstract (bool IsValid, TSource Value, string? error) ConvertFrom(string? text);
    public StringValidator(string sourcePropertyName, Action<TSource> assignToSource): base(sourcePropertyName)
    {
        this.assignToSource = assignToSource;
    }
    public void UpdateText(string? text)
    {
        var (isValid, value, error) = ConvertFrom(text);
        if (isValid)
        {
            assignToSource(value);
            Errors = ImmutableArray<string>.Empty;
        }
        else
        {
            Errors = ImmutableArray<string>.Empty.Add(error ?? "Unknown error");
        }
        TextValue = text;
    }
}
