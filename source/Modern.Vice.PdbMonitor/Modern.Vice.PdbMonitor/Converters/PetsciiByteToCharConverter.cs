namespace Modern.Vice.PdbMonitor.Converters;
public static class PetsciiMap
{
    public static char ToUnicode(byte b) => (char)(0xE000 + b);
}
public class PetsciiByteToCharConverter : ParameterlessValueConverter<byte, string>
{
    public override string? Convert(byte value, Type targetType, CultureInfo culture)
    {
        return new string(PetsciiMap.ToUnicode(value), 1);
    }

    public override byte ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
