namespace Modern.Vice.PdbMonitor.Converters;

public class ByteToCharConverter : ParameterlessValueConverter<byte, string>
{
    public override string? Convert(byte value, Type targetType, CultureInfo culture)
    {
        return new string((char)value, 1);
    }

    public override byte ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
