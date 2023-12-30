using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Converters;
public class PdbVariableAddressRangeToStringConverter : ParameterlessValueConverter<PdbVariable, string>
{
    public override string? Convert(PdbVariable? value, Type targetType, CultureInfo culture)
    {
        if (value is not null)
        {
            return $"${value.Start:x4}-${value.End:x4}";
        }
        return null;
    }

    public override PdbVariable? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
