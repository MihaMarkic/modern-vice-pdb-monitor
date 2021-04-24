using System;
using System.Globalization;

namespace Modern.Vice.PdbMonitor.Converters
{
    public class TabsToSpacesConverter : ParameterlessValueConverter<string, string>
    {
        static readonly string space = new string(' ', 4);
        public override string? Convert(string? value, Type targetType, CultureInfo culture)
        {
            return value?.Replace("\t", space);
        }

        public override string? ConvertBack(string? value, Type targetType, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
