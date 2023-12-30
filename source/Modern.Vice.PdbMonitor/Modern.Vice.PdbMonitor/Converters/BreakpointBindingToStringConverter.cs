using Modern.Vice.PdbMonitor.Engine.Models;

namespace Modern.Vice.PdbMonitor.Converters;
public class BreakpointBindingToStringConverter : ParameterlessValueConverter<BreakpointBind, string>
{
    public override string? Convert(BreakpointBind? value, Type targetType, CultureInfo culture)
    {
        if (value is not null && value is not BreakpointNoBind)
        {
            return value.ToString();
        }
        return null;
    }

    public override BreakpointBind? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
