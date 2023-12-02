using Righthand.ViceMonitor.Bridge.Commands;

namespace Modern.Vice.PdbMonitor.Converters;
public class ViceCommandTypeToTextConverter : ParameterlessValueConverter<IViceCommand?, string>
{
    public override string? Convert(IViceCommand? value, Type targetType, CultureInfo culture)
    {
        if (value is null)
        {
            return null;
        }
        return value.GetType().Name.Replace("Command", "");
    }

    public override IViceCommand? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
