using System;
using System.Globalization;

namespace Modern.Vice.PdbMonitor.Converters;

public class HexAddressConverter : ParameterlessValueConverter<ushort?, string>
{
    public bool TrimToByte { get; set; }
    public override string? Convert(ushort? value, Type targetType, CultureInfo culture)
    {
        if (value.HasValue)
        {
            if (TrimToByte && value.Value < 256)
            {
                return $"${value.Value:x2}";
            }
            return $"${value.Value:x4}";
        }
        return string.Empty;
    }

    public override ushort? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
