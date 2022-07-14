using System;
using System.Collections.Generic;
using System.Globalization;

namespace Modern.Vice.PdbMonitor.Converters;

public class ArrayToTextConverter : ParameterlessValueConverter<IEnumerable<object>, string>
{
    public override string? Convert(IEnumerable<object>? value, Type targetType, CultureInfo culture)
    {
        if (value is not null)
        {
            return string.Join(Environment.NewLine, value);
        }
        return null;
    }

    public override IEnumerable<object>? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
