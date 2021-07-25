using System;
using System.Collections.Immutable;
using Modern.Vice.PdbMonitor.Core;

namespace Modern.Vice.PdbMonitor.Engine.BindingValidators
{
    /// <summary>
    /// Provides validation of bound value before it is being converted.
    /// ViewModel should route a string value to <see cref="TextValue"/> property and track <see cref="HasErrorsChanged"/> event.
    /// </summary>
    /// <typeparam name="TSource">Source type for UI string value.</typeparam>
    public abstract class StringValidator<TSource> : NotifiableObject, IBindingValidator
    {
        readonly Action<TSource> assignToSource;
        public string SourcePropertyName { get; }
        public string? TextValue { get; private set; }
        public event EventHandler? HasErrorsChanged;
        public ImmutableArray<string> Errors { get; private set; } = ImmutableArray<String>.Empty;
        public bool HasErrors => !Errors.IsDefaultOrEmpty;
        public abstract string ConvertTo(TSource source);
        public abstract (bool IsValid, TSource Value, string? error) ConvertFrom(string? text);
        public StringValidator(string sourcePropertyName, TSource initialValue, Action<TSource> assignToSource)
        {
            SourcePropertyName = sourcePropertyName;
            TextValue = ConvertTo(initialValue);
            this.assignToSource = assignToSource;
        }
        protected virtual void OnHasErrorsChanged(EventArgs e) => HasErrorsChanged?.Invoke(this, e);
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
}
