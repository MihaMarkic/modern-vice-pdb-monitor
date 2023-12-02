namespace Modern.Vice.PdbMonitor.Converters;
public class ByteArrayToTextConverter : ParameterlessValueConverter<IEnumerable<byte>, string>
{
    public override string? Convert(IEnumerable<byte>? data, Type targetType, CultureInfo culture)
    {
        if (data is null)
        {
            return null;
        }
        return string.Join(" ", data.Select(d => d.ToString("X2")));
    }

    public override IEnumerable<byte>? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
