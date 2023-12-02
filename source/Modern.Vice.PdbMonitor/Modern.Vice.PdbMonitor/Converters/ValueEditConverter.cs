namespace Modern.Vice.PdbMonitor.Converters;

public abstract class ValueEditConverter<TValue> : ParameterlessValueConverter<TValue, string>
    where TValue : struct
{
    public override TValue ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        if (value is null)
        {
            return default;
        }
        return ConvertValueBack(value);
    }
    protected abstract TValue ConvertValueBack(string value);
}
public abstract class ConvertNumericValue<TValue> : ValueEditConverter<TValue>
    where TValue : struct
{
    public override string? Convert(TValue value, Type targetType, CultureInfo culture)
    {
        return value.ToString();
    }
}
public class Uint16ValueEditConverter : ConvertNumericValue<ushort>
{
    protected override ushort ConvertValueBack(string value)
    {
        return UInt16.TryParse(value, out var result) ? result : default;
    }
}
