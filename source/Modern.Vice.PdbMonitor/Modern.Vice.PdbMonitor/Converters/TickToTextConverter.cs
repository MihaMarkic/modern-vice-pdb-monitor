
namespace Modern.Vice.PdbMonitor.Converters;
public class TickToTextConverter : ParameterlessValueConverter<long?, string>
{
    public override string? Convert(long? value, Type targetType, CultureInfo culture)
    {
        if (value is null)
        {
            return null;
        }
        var ms = TimeSpan.FromTicks(value.Value).TotalMilliseconds;
        return $"{ms:#,##0}";
    }

    public override long? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
