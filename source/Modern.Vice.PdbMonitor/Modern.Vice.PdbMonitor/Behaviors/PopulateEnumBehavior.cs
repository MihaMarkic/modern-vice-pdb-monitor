using System;
using System.Collections.Immutable;
using System.Linq;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Behaviors;

public class PopulateEnumBehavior : ClassicBehavior<ComboBox>
{
    static readonly EnumDisplayTextMapper mapper;
    static PopulateEnumBehavior()
    {
        mapper = IoC.Host.Services.GetRequiredService<EnumDisplayTextMapper>();
    }
    public Type? Type { get; set; }
    protected override void Attached()
    {
        if (Type != null)
        {
            var items = mapper.GetMapEnum(Type);
            AssociatedObject!.Items = items
                .OrderBy(i => i.Key)
                .Select(i => new ComboBoxKeyValueItem (i.Key, i.Value)).ToImmutableArray();
        }
        base.Attached();
    }
}

public record ComboBoxKeyValueItem(object Key, string Text);
