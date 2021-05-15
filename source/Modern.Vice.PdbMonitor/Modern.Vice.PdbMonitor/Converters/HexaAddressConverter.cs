using System;
using System.Globalization;

namespace Modern.Vice.PdbMonitor.Converters
{
    public class HexAddressConverter : ParameterlessValueConverter<ushort?, string>
    {
        public override string? Convert(ushort? value, Type targetType, CultureInfo culture)
        {
            if (value.HasValue)
            {
                return $"${value.Value:x4}";
            }
            return string.Empty;
        }

        public override ushort? ConvertBack(string? value, Type targetType, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
