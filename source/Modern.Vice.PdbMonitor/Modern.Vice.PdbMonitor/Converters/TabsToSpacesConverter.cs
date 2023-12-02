namespace Modern.Vice.PdbMonitor.Converters;

public class TabsToSpacesConverter : ParameterlessValueConverter<string, string>
{
    public int Spaces { get; set; } = 4;
    public override string? Convert(string? value, Type targetType, CultureInfo culture)
    {
        return value.ConvertTabsToSpaces(Spaces);
    }
    public override string? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
