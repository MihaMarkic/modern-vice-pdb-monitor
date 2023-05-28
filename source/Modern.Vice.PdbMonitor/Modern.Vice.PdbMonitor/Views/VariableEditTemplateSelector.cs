using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Views;
public class VariableEditTemplateSelector : IDataTemplate
{
    [Content]
    public Dictionary<string, IDataTemplate> Templates { get; } = new Dictionary<string, IDataTemplate>();
    public Control? Build(object? param)
    {
        var slot = (VariableSlot?)param ?? throw new ArgumentNullException(nameof(param));
        string key;
        if (slot.Source.Type is PdbValueType valueType)
        {
            if (valueType is PdbEnumType)
            {
                key = "Enum";
            }
            else
            {
                if (valueType.VariableType == PdbVariableType.Bool)
                {
                    key = "Bool";
                }
                else
                {
                    key = "ValueType";
                }
            }
        }
        else
        {
            return null;
        }
        if (Templates.TryGetValue(key, out var template))
        {
            return template.Build(param);
        }
        return null;
    }

    public bool Match(object? data) => data is VariableSlot;
}
