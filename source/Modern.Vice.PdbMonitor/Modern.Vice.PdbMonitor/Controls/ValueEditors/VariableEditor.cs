using Avalonia;
using Avalonia.Controls;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Controls.ValueEditors;

public abstract class VariableEditor : UserControl
{
    public static readonly DirectProperty<VariableEditor, VariablesViewModel?> ViewModelProperty =
        AvaloniaProperty.RegisterDirect<VariableEditor, VariablesViewModel?>(nameof(VariableSlot),
            o => o.ViewModel, (o, v) => o.ViewModel = v);
    VariablesViewModel? viewModel;
    public VariablesViewModel? ViewModel
    {
        get => viewModel;
        set => SetAndRaise(ViewModelProperty, ref viewModel, value);
    }
    protected VariableSlot? VariableSlot => (VariableSlot?)DataContext;
    public async Task SaveValueAsync(CancellationToken ct = default)
    {
        if (IsValueValid)
        {
            var variablesViewModel = ViewModel ?? throw new NullReferenceException("Invalid bound view model");
            var variableSlot = VariableSlot ?? throw new NullReferenceException("VariableSlot not set");
            await variablesViewModel.UpdateVariableValueAsync(variableSlot, FinalValue, ct);
        }
    }
    protected virtual bool IsValueValid => true;
    protected abstract object FinalValue { get; }
}
