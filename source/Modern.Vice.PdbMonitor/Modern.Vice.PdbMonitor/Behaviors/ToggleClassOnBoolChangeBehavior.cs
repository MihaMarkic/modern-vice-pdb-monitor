using Avalonia;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace Modern.Vice.PdbMonitor.Behaviors
{
    public class ToggleClassOnBoolChangeBehavior : AvaloniaObject
    {
        public static readonly AttachedProperty<bool> TriggerProperty = AvaloniaProperty.RegisterAttached<ToggleClassOnBoolChangeBehavior, Interactive, bool>(
            "Trigger", default!, false, BindingMode.OneWay, coerce: ValidateTrigger);
        public static readonly AttachedProperty<string> ClassProperty = AvaloniaProperty.RegisterAttached<ToggleClassOnBoolChangeBehavior, Interactive, string>(
            "Class", default!, false, BindingMode.OneWay, coerce: ValidateClass);
        public static void SetTrigger(IAvaloniaObject element, bool value) => element.SetValue(TriggerProperty, value);
        public static bool GetTrigger(IAvaloniaObject element) => element.GetValue(TriggerProperty);
        public static void SetClass(IAvaloniaObject element, string value) => element.SetValue(ClassProperty, value);
        public static string GetClass(IAvaloniaObject element) => element.GetValue(ClassProperty);
        public static bool ValidateTrigger(IAvaloniaObject element, bool value)
        {
            UpdateTarget(element, value, GetClass(element));
            return value;
        }
        public static string ValidateClass(IAvaloniaObject element, string value)
        {
            UpdateTarget(element, GetTrigger(element), value);
            return value;
        }
        internal static void UpdateTarget(IAvaloniaObject element, bool value, string className)
        {
            if (element is StyledElement styled && !string.IsNullOrWhiteSpace(className))
            {
                if (value)
                {
                    styled.Classes.Add(className);
                }
                else
                {
                    styled.Classes.Remove(className);
                }
            }
        }
    }
}
