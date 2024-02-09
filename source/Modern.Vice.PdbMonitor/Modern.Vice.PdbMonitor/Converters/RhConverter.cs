using System.ComponentModel;

namespace Modern.Vice.PdbMonitor.Converters;

public static class RhConverter
{
    static readonly Dictionary<Type, TypeConverter> converters = new Dictionary<Type, TypeConverter>();

    internal static object? ConvertToObject<T>(object? source)
    {
        if (source is null)
        {
            return null;
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
                    return converter.ConvertFrom(null, CultureInfo.InvariantCulture, source);
                }
                catch
                {
                    return null;
                }
            }
        }
        return null;
    }
}
