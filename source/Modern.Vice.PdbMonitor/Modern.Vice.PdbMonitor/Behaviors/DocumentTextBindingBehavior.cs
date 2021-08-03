using System;
using Avalonia;
using AvaloniaEdit;

namespace Modern.Vice.PdbMonitor.Behaviors
{
    public class DocumentTextBindingBehavior : ClassicBehavior<TextEditor>
    {
        public static readonly StyledProperty<string?> TextProperty =
            AvaloniaProperty.Register<DocumentTextBindingBehavior, string?>(nameof(Text));

        public DocumentTextBindingBehavior()
        {
            this.GetObservable(TextProperty).Subscribe(TextPropertyChanged);
        }

        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        protected override void Attached()
        {
            base.Attached();
            AssociatedObject!.TextChanged += TextChanged;
        }
        protected override void Detached()
        {
            base.Detached();
            AssociatedObject!.TextChanged -= TextChanged;
        }

        void TextChanged(object? sender, EventArgs eventArgs)
        {
            if (AssociatedObject?.Document != null)
            {
                Text = AssociatedObject.Document.Text;
            }
        }

        void TextPropertyChanged(string? text)
        {
            if (AssociatedObject?.Document != null && text != null && !string.Equals(text, AssociatedObject.Document.Text, StringComparison.Ordinal))
            {
                var caretOffset = AssociatedObject.CaretOffset;
                AssociatedObject.Document.Text = text;
                AssociatedObject.CaretOffset = caretOffset;
            }
        }
    }
}
