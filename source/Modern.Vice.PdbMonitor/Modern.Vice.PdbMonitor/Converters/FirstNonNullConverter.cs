using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace Modern.Vice.PdbMonitor.Converters;

public class FirstNonNullConverter : IMultiValueConverter
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns>Returns first non null value</returns>
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values?.Count > 0)
        {
            return values.First(v => v is not null);
        }
        return null;
    }
}
