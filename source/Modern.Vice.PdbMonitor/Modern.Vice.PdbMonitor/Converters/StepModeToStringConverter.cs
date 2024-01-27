using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Converters;
public class StepModeToStringConverter : ParameterlessValueConverter<DebuggerStepMode, string>
{
    public override string? Convert(DebuggerStepMode value, Type targetType, CultureInfo culture)
    {
        return value switch
        {
            DebuggerStepMode.Assembly => "ASM",
            DebuggerStepMode.High => "HIGH",
            _ => "?"
        };
    }

    public override DebuggerStepMode ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
