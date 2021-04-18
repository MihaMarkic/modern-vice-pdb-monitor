using Avalonia.Data.Converters;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Modern.Vice.PdbMonitor.Converters
{
    public abstract class ParameterlessValueConverter<TSource, TDest> : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isConvertible = IsConvertibleToDest(value);
            if (isConvertible)
            {
                var sourceValue = (TSource?)value;
                if (WillConvert(sourceValue))
                {
                    return Convert(sourceValue, targetType, culture);
                }
                else
                {
                    return default;
                }
            }
            else
            {
                string valueTypeName = !ReferenceEquals(value, null) ? value.GetType().Name : "Unknown";
                Debug.WriteLine($"Can't convert value {value} of type {valueTypeName} to type {typeof(TSource).Name}");
                return default(TDest);
            }
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TDest dest)
            {
                return ConvertBack(dest, targetType, culture);
            }
            else
            {
                return default(TSource);
            }
        }
        public static bool IsConvertibleToDest(object? value)
        {
            bool isConvertible = value is TSource;

            if (!isConvertible)
            {
                Type sourceType = typeof(TSource);
                isConvertible = value is null && (
                        (sourceType.IsClass || typeof(TSource).IsInterface)
                        || (sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(Nullable<>)));
            }
            return isConvertible;
        }
        protected virtual bool WillConvert(TSource? value) => true;
        public abstract TDest? Convert(TSource? value, Type targetType, CultureInfo culture);
        public abstract TSource? ConvertBack(TDest? value, Type targetType, CultureInfo culture);
    }
}
