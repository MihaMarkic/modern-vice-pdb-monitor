using System;
using System.Globalization;

namespace Modern.Vice.PdbMonitor.Converters;

public class HexAddressConverter : ParameterlessValueConverter<ushort?, string>
{
    public bool TrimToByte { get; set; }
    public string Prefix { get; set; } = "$";
    public override string? Convert(ushort? value, Type targetType, CultureInfo culture)
    {
        if (value.HasValue)
        {
            if (TrimToByte && value.Value < 256)
            {
                return $"{Prefix}{value.Value:x2}";
            }
            return $"{Prefix}{value.Value:x4}";
        }
        return string.Empty;
    }

    public override ushort? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        if (value is null)
        {
            return null;
        }
        return ushort.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort hex)
            ? hex: (ushort?)null;
    }
}
