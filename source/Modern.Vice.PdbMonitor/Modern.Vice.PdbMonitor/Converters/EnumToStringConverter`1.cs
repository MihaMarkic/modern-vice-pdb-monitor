namespace Modern.Vice.PdbMonitor.Converters;

public abstract class EnumToDescriptionConverter<TEnum> : ParameterlessValueConverter<TEnum, string>
    where TEnum : Enum
{
    public override string? Convert(TEnum? value, Type targetType, CultureInfo culture)
    {
        return value?.GetDisplayText();
    }
    public override TEnum? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
