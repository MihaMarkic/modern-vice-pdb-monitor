using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;

namespace System;

public static class AttributeExtension
{
    static readonly EnumDisplayTextMapper mapper;
    static AttributeExtension()
    {
        mapper = IoC.Host.Services.GetRequiredService<EnumDisplayTextMapper>();
    }
    public static string? GetDisplayText<TEnum>(this TEnum value)
        where TEnum : Enum
    {
        var map = mapper.GetMapEnum(typeof(TEnum));
        return map[value];
    }
}
