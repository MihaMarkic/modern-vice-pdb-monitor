using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Modern.Vice.PdbMonitor.Converters;

public abstract class ValueConverter<TSource, TDest, TParam> : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TSource)
        {
            TParam? param = RhConverter.ConvertFrom<TParam>(parameter);
            return Convert((TSource)value, targetType, param, culture);
        }
        else
            return default(TDest);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TDest)
        {
            TParam? param = RhConverter.ConvertFrom<TParam>(parameter);
            return ConvertBack((TDest)value, targetType, param, culture);
        }
        else
            return default(TSource);
    }

    public abstract TDest? Convert(TSource? value, Type targetType, TParam? parameter, CultureInfo culture);
    public abstract TSource? ConvertBack(TDest? value, Type targetType, TParam? parameter, CultureInfo culture);
}
