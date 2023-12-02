namespace Modern.Vice.PdbMonitor.Converters;

public abstract class IntThresholdToConverter<T> : ParameterlessValueConverter<int, T>
{
    public int Threshold { get; set; }
    public T? OverOrEqualThreshold { get; set; }
    public T? UnderThreshold { get; set; }

    public override T? Convert(int value, Type targetType, CultureInfo culture)
    {
        return value < Threshold ? UnderThreshold : OverOrEqualThreshold;
    }
    public override int ConvertBack(T? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
