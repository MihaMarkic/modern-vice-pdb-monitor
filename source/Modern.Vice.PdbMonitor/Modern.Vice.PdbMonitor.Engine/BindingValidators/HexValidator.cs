using System;

namespace Modern.Vice.PdbMonitor.Engine.BindingValidators
{
    /// <summary>
    /// Converts string hex value to ushort source and the other way round.
    /// </summary>
    public class HexValidator : StringValidator<ushort>
    {
        readonly int digits;
        readonly string hexFormat;
        public HexValidator(string sourcePropertyName, ushort initialValue, int digits, Action<ushort> assignToSource) 
            : base(sourcePropertyName, initialValue, assignToSource)
        {
            this.digits = digits;
            hexFormat = $"X{digits}";
        }

        public override (bool IsValid, ushort Value, string? error) ConvertFrom(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return (false, default, "Input can not be empty");
            }
            if (!ushort.TryParse(text, out ushort value))
            {
                return (false, default, "Input is not a valid hex value");
            }
            return (true, value, null);
        }

        public override string ConvertTo(ushort source)
        {
            return source.ToString(hexFormat);
        }
    }
}
