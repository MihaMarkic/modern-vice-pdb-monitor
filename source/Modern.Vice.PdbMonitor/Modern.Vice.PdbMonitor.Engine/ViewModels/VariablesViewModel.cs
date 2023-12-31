using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core.Common;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public class VariablesViewModel: VariablesCoreViewModel
{
    public VariablesViewModel(ILogger<VariablesViewModel> logger, IViceBridge viceBridge, IDispatcher dispatcher, 
        EmulatorMemoryViewModel emulatorMemoryViewModel, ExecutionStatusViewModel executionStatusViewModel, Globals globals) 
        : base(logger, viceBridge, dispatcher, emulatorMemoryViewModel, executionStatusViewModel, globals)
    {
    }
    /// <summary>
    /// Creates slots for both function and global variables. Values are fetched asynchronously.
    /// </summary>
    /// <param name="line"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public void UpdateForLine(PdbLine line)
    {
        ClearVariables();
        var lineVariables = line.Variables;
        var globalVariables = globals.Project?.DebugSymbols?.GlobalVariablesMap ?? ImmutableDictionary<string, PdbVariable>.Empty;
        if (!lineVariables.IsEmpty || !globalVariables.IsEmpty)
        {
            var mapBuilder = ImmutableDictionary.CreateBuilder<PdbVariable, VariableSlot>();
            var slots = new List<VariableSlot>();
            foreach (var variable in lineVariables.Values)
            {
                var slot = new VariableSlot(variable, isGlobal: false);
                slots.Add(slot);
                mapBuilder.Add(variable, slot);
            }
            foreach (var variable in globalVariables)
            {
                var slot = new VariableSlot(variable.Value, isGlobal: true);
                slots.Add(slot);
                mapBuilder.Add(variable.Value, slot);
            }
            foreach (var slot in slots.OrderBy(s => s.Name))
            {
                Items.Add(slot);
            }
            var map = mapBuilder.ToImmutable();
            FillVariableValues(map);
        }
    }
}
