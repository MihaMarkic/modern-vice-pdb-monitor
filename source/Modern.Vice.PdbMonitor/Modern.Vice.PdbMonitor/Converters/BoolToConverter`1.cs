using System;
using System.Globalization;

namespace Modern.Vice.PdbMonitor.Converters;

public abstract class BoolToConverter<T> : ParameterlessValueConverter<bool, T>
{
    public T? OnFalse { get; set; }
    public T? OnTrue { get; set; }
    public bool ApplyOnTrue { get; set; }
    public bool ApplyOnFalse { get; set; }
    public BoolToConverter()
    {
        ApplyOnTrue = ApplyOnFalse = true;
    }
    protected override bool WillConvert(bool value)
    {
        return value && ApplyOnTrue || !value && ApplyOnFalse;
    }
    public override T? Convert(bool value, Type targetType, CultureInfo culture)
    {
        return value ? OnTrue : OnFalse;
    }

    public override bool ConvertBack(T? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

}
