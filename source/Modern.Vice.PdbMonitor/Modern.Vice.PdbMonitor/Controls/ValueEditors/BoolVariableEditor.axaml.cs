using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using System;

namespace Modern.Vice.PdbMonitor.Controls.ValueEditors;
public partial class BoolVariableEditor : VariableEditor
{
    public bool Value { get; set; }
    public BoolVariableEditor()
    {
        InitializeComponent();
    }
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (VariableSlot?.Source.Type is PdbValueType valueType
            && valueType.VariableType == PdbVariableType.Bool)
        {
            var binding = new Binding(nameof(Value), BindingMode.TwoWay)
            {
                Source = this,
            };
            Value = (bool)VariableSlot.Value!.CoreValue!;
            AvaloniaObjectExtensions.Bind(Editor, CheckBox.IsCheckedProperty, binding);
        }
        base.OnDataContextChanged(e);
    }
    protected override object FinalValue => Value;
}
