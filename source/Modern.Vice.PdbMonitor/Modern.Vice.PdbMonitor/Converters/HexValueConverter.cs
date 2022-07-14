using System;
using System.Globalization;

namespace Modern.Vice.PdbMonitor.Converters;

/// <summary>
/// Formats hex number based on number type
/// </summary>
public class HexValueConverter : ParameterlessValueConverter<object?, string?>
{
    public override string? Convert(object? value, Type targetType, CultureInfo culture)
    {
        return value switch
        {
            ushort us => us.ToString("x4"),
            byte by => by.ToString("x2"),
            _ => "",
        };
    }

    public override object? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
