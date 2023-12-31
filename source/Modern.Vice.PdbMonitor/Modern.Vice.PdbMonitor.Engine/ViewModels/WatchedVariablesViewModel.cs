using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core.Common;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public class WatchedVariablesViewModel: VariablesCoreViewModel
{
    public WatchedVariablesViewModel(ILogger<WatchedVariablesViewModel> logger, IViceBridge viceBridge, IDispatcher dispatcher, 
        EmulatorMemoryViewModel emulatorMemoryViewModel, ExecutionStatusViewModel executionStatusViewModel, Globals globals) 
        : base(logger, viceBridge, dispatcher, emulatorMemoryViewModel, executionStatusViewModel, globals)
    {
    }

    public void AddVariable(PdbVariable variable)
    {
        if (Items.Any(i => i.Source == variable))
        {
            return;
        }
        var globalVariables = globals.Project?.DebugSymbols?.GlobalVariables ?? ImmutableHashSet<PdbVariable>.Empty;
        bool isGlobal = globalVariables.Contains(variable);
        var slot = new VariableSlot(variable, isGlobal: isGlobal);
        Items.Add(slot);
        FillVariableValue(slot, variable);
    }

    public void ClearValues()
    {
        foreach (var slot in Items)
        {
            slot.ClearValue();
        }
    }

    public void UpdateValues()
    {
        foreach (var slot in Items)
        {
            FillVariableValue(slot, slot.Source);
        }
    }
}
