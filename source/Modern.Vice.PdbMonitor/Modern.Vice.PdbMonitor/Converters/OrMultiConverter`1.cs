using System;
using System.Globalization;
using Avalonia.Data.Converters;
using System.Linq;
using System.Collections.Generic;

namespace Modern.Vice.PdbMonitor.Converters;

public abstract class OrMultiConverter<T> : IMultiValueConverter
{
    public T? OnTrue { get; set; }
    public T? OnFalse { get; set; }
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values?.Any(v => v is null || v is not bool) ?? false)
        {
            return null;
        }
        bool result = values!.Any(v => (bool)v!);
        return result ? OnTrue : OnFalse;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
