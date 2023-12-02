using Avalonia.Data.Converters;

namespace Modern.Vice.PdbMonitor.Converters;
public class PassthroughConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}
