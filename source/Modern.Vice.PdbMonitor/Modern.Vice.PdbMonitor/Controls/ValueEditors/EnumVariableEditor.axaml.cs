using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Controls.ValueEditors;
public partial class EnumVariableEditor : VariableEditor
{
    public int ValueIndex { get; set; }
    ImmutableArray<KeyValuePair<string, uint>> values;
    public EnumVariableEditor()
    {
        InitializeComponent();
    }
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (VariableSlot?.Source.Type is PdbEnumType enumType)
        {
            var binding = new Binding(nameof(ValueIndex), BindingMode.TwoWay)
            {
                Source = this,
            };

            values = enumType.ByValue.OrderBy(p => p.Key).Select(p => new KeyValuePair<string, uint>(p.Key, (uint)p.Value))
                .ToImmutableArray();
            Editor.Items.Clear();
            foreach (var p in values)
            {
                Editor.Items.Add(p.Key);
            }
            uint value = Convert.ToUInt32(VariableSlot.Value!.CoreValue!);
            for (int i=0; i<values.Length; i++)
            {
                if (values[i].Value == value)
                {
                    ValueIndex = i;
                    break;
                }
            }
            AvaloniaObjectExtensions.Bind(Editor, ComboBox.SelectedIndexProperty, binding);
        }
        base.OnDataContextChanged(e);
    }
    protected override object FinalValue => values[ValueIndex].Value;
}
