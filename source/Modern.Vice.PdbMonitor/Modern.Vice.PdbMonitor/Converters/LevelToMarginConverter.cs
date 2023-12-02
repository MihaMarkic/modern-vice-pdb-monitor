using Avalonia;

namespace Modern.Vice.PdbMonitor.Converters;
public class LevelToMarginConverter : ValueConverter<int, Thickness, double?>
{
    public override Thickness Convert(int value, Type targetType, double? parameter, CultureInfo culture)
    {
        return new Thickness((parameter ?? 10) * value, 0, 0, 0);
    }

    public override int ConvertBack(Thickness value, Type targetType, double? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
