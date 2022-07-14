using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Modern.Vice.PdbMonitor.Converters;

public static class RhConverter
{
    static readonly Dictionary<Type, TypeConverter> converters = new Dictionary<Type, TypeConverter>();

    public static T? ConvertFrom<T>(object source)
    {
        T? result;
        if (source is T)
        {
            result = (T)source;
        }
        else
        {
            if (source is null)
            {
                result = default;
            }
            else
            {
                TypeConverter? converter;
                if (!converters.TryGetValue(typeof(T), out converter))
                {
                    converter = TypeDescriptor.GetConverter(typeof(T));
                    converters.Add(typeof(T), converter);
                }
                if (converter.CanConvertFrom(source.GetType()))
                {
                    try
                    {
                        result = (T)converter.ConvertFrom(null, CultureInfo.InvariantCulture, source);
                    }
                    catch
                    {
                        result = default;
                    }
                }
                else
                {
                    result = default;
                }
            }
        }
        return result;
    }
}
