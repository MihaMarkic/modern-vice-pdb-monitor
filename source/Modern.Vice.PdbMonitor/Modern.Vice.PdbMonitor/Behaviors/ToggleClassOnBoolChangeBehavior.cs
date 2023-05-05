using Avalonia;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace Modern.Vice.PdbMonitor.Behaviors;

public class ToggleClassOnBoolChangeBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<bool> TriggerProperty = AvaloniaProperty.RegisterAttached<ToggleClassOnBoolChangeBehavior, Interactive, bool>(
        "Trigger", default!, false, BindingMode.OneWay, coerce: ValidateTrigger);
    public static readonly AttachedProperty<string> ClassProperty = AvaloniaProperty.RegisterAttached<ToggleClassOnBoolChangeBehavior, Interactive, string>(
        "Class", default!, false, BindingMode.OneWay, coerce: ValidateClass);
    public static void SetTrigger(AvaloniaObject element, bool value) => element.SetValue(TriggerProperty, value);
    public static bool GetTrigger(AvaloniaObject element) => element.GetValue(TriggerProperty);
    public static void SetClass(AvaloniaObject element, string value) => element.SetValue(ClassProperty, value);
    public static string GetClass(AvaloniaObject element) => element.GetValue(ClassProperty);
    public static bool ValidateTrigger(AvaloniaObject element, bool value)
    {
        UpdateTarget(element, value, GetClass(element));
        return value;
    }
    public static string ValidateClass(AvaloniaObject element, string value)
    {
        UpdateTarget(element, GetTrigger(element), value);
        return value;
    }
    internal static void UpdateTarget(AvaloniaObject element, bool value, string className)
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
