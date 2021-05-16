using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Modern.Vice.PdbMonitor.Engine.Models;

namespace Modern.Vice.PdbMonitor.Converters
{
    public class FlagsConverter : ParameterlessValueConverter<byte?, ImmutableArray<FlagModel>>
    {
        public override ImmutableArray<FlagModel> Convert(byte? value, Type targetType, CultureInfo culture)
        {
            if (value.HasValue)
            {
                const string flagNames = "NV-BDIZC";
                var flags = flagNames
                    .Select((c, i) => new FlagModel(new string(c, 1), ((value.Value >> 7 - i) & 1) == 1))
                    .ToImmutableArray();
                return flags;
            }
            return ImmutableArray<FlagModel>.Empty;
        }

        public override byte? ConvertBack(ImmutableArray<FlagModel> value, Type targetType, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    
}
