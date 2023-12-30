namespace Modern.Vice.PdbMonitor.Converters;
public class UIntArrayToTextConverter : ParameterlessValueConverter<IEnumerable<uint>, string>
{
    public override string? Convert(IEnumerable<uint>? data, Type targetType, CultureInfo culture)
    {
        if (data is null)
        {
            return null;
        }
        return string.Join(" ", data.Select(d => d.ToString("X2")));
    }

    public override IEnumerable<uint>? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
