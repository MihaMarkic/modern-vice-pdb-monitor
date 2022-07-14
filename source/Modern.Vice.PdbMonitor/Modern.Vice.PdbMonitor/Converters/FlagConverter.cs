using System;
using System.Globalization;

namespace Modern.Vice.PdbMonitor.Converters;

/// <summary>
/// Converts byte to flag
/// </summary>
public class FlagConverter : ValueConverter<byte?, bool?, int>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter">Which bit to return. Valid range is 0..7</param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public override bool? Convert(byte? value, Type targetType, int parameter, CultureInfo culture)
    {
        if (value.HasValue && parameter >= 0 && parameter < 8)
        {
            return ((value.Value >> parameter) & 1) == 1;
        }
        return null;
    }

    public override byte? ConvertBack(bool? value, Type targetType, int parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
