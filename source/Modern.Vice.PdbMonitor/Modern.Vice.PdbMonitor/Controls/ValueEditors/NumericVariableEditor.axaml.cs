using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Controls.ValueEditors;
public partial class NumericVariableEditor: VariableEditor
{
    protected IEditValueViewModel? editValueViewModel;
    public NumericVariableEditor()
    {
        InitializeComponent();
    }
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (VariableSlot?.Source.Type is PdbValueType valueType)
        {
            editValueViewModel = EditValueViewModelFactory.CreateFor(valueType.VariableType);
            var binding = new Binding(nameof(editValueViewModel.ValueText), BindingMode.TwoWay)
            {
                Source = editValueViewModel,
            };
            editValueViewModel.ValueText = VariableSlot.VariableValue?.ToString() ?? "";
            AvaloniaObjectExtensions.Bind(Editor, TextBox.TextProperty, binding);
        }
        base.OnDataContextChanged(e);
    }
    protected override bool IsValueValid => editValueViewModel?.HasErrors == false;
    protected override object FinalValue => editValueViewModel?.Value 
        ?? throw new NullReferenceException("EditValueViewModel not set");
}
