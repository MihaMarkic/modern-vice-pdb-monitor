using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public interface IEditValueViewModel
{
    string ValueText { get; set; }
    object Value { get; }
    bool HasErrors { get; }
}
public abstract class EditValueViewModel<T> : NotifiableObject, IEditValueViewModel, INotifyDataErrorInfo
    where T : struct, IParsable<T>
{
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public bool IsHex { get; set; }
    public bool HasErrors { get; private set; }
    public T Value { get; private set; }
    object IEditValueViewModel.Value => Value;
    public string ValueText { get; set; } = "";
    public IEnumerable GetErrors(string? propertyName)
    {
        if (HasErrors)
        {
            yield return "Not valid number";
        }
    }
    protected override void OnPropertyChanged([CallerMemberName]string name = default!)
    {
        switch (name)
        {
            case nameof(ValueText):
                var numberStyles = IsHex ? NumberStyles.HexNumber : NumberStyles.None;
                bool hasError = !TryParse(ValueText, numberStyles, out var result);
                if (!hasError)
                {
                    Value = result;
                }
                if (hasError != HasErrors)
                {
                    HasErrors = hasError;
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(ValueText)));
                }
                break;
        }
        base.OnPropertyChanged(name);
    }

    internal abstract bool TryParse(string text, NumberStyles numberStyles, out T value);
}

public static class EditValueViewModelFactory
{
    public static IEditValueViewModel CreateFor(PdbVariableType variableType)
    {
        return variableType switch
        {
            PdbVariableType.Byte => new SByteEditValueViewModel(),
            PdbVariableType.UByte => new ByteEditValueViewModel(),
            PdbVariableType.Int16 => new Int16EditValueViewModel(),
            PdbVariableType.UInt16 => new UInt16EditValueViewModel(),
            PdbVariableType.Int32 => new Int32EditValueViewModel(),
            PdbVariableType.UInt32 => new UInt32EditValueViewModel(),
            PdbVariableType.Float => new FloatEditValueViewModel(),
            _ => throw new ArgumentException($"Varible type {variableType} is not supported", nameof(variableType)),
        };
    }
}

// ** Linqpad generator script
//var types = new string[] { "UInt16", "Int16", "SByte", "Byte", "UInt32", "Int32" };
//foreach (var type in types)
//{
//	$$"""
//	public class {{type}}EditValueViewModel: EditValueViewModel<{{type}}>
//	{
//	    internal override bool TryParse(string text, NumberStyles numberStyles, out {{type}} value)
//	    {
//	        return {{type}}.TryParse(text, numberStyles, CultureInfo.InvariantCulture, out value);
//	    }
//	}
//	""".Dump();
//}
public class UInt16EditValueViewModel : EditValueViewModel<UInt16>
{
    internal override bool TryParse(string text, NumberStyles numberStyles, out UInt16 value)
    {
        return UInt16.TryParse(text, numberStyles, CultureInfo.InvariantCulture, out value);
    }
}
public class Int16EditValueViewModel : EditValueViewModel<Int16>
{
    internal override bool TryParse(string text, NumberStyles numberStyles, out Int16 value)
    {
        return Int16.TryParse(text, numberStyles, CultureInfo.InvariantCulture, out value);
    }
}
public class SByteEditValueViewModel : EditValueViewModel<SByte>
{
    internal override bool TryParse(string text, NumberStyles numberStyles, out SByte value)
    {
        return SByte.TryParse(text, numberStyles, CultureInfo.InvariantCulture, out value);
    }
}
public class ByteEditValueViewModel : EditValueViewModel<Byte>
{
    internal override bool TryParse(string text, NumberStyles numberStyles, out Byte value)
    {
        return Byte.TryParse(text, numberStyles, CultureInfo.InvariantCulture, out value);
    }
}
public class UInt32EditValueViewModel : EditValueViewModel<UInt32>
{
    internal override bool TryParse(string text, NumberStyles numberStyles, out UInt32 value)
    {
        return UInt32.TryParse(text, numberStyles, CultureInfo.InvariantCulture, out value);
    }
}
public class Int32EditValueViewModel : EditValueViewModel<Int32>
{
    internal override bool TryParse(string text, NumberStyles numberStyles, out Int32 value)
    {
        return Int32.TryParse(text, numberStyles, CultureInfo.InvariantCulture, out value);
    }
}
public class FloatEditValueViewModel: EditValueViewModel<float>
{
    internal override bool TryParse(string text, NumberStyles numberStyles, out float value)
    {
        return float.TryParse(text, numberStyles, CultureInfo.InvariantCulture, out value);
    }
}
