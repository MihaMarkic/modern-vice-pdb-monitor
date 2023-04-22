using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Extensions;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public class VariablesViewModel: NotifiableObject
{
    readonly ILogger<VariablesViewModel> logger;
    readonly IViceBridge viceBridge;
    readonly IDispatcher dispatcher;
    readonly TaskFactory uiFactory;
    readonly Globals globals;
    readonly CommandsManager commandsManager;
    public RelayCommand<VariableSlot> ToggleVariableExpansionCommand { get; }
    public ObservableCollection<VariableSlot> Items { get; } = new ObservableCollection<VariableSlot>();
    public bool IsWorking => IsWorkingCount > 0;
    int IsWorkingCount { get; set; }
    public VariablesViewModel(ILogger<VariablesViewModel> logger, IViceBridge viceBridge,
        IDispatcher dispatcher, Globals globals)
    {
        this.logger = logger;
        this.viceBridge = viceBridge;
        this.dispatcher = dispatcher;
        uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        this.globals = globals;
        commandsManager = new CommandsManager(this, new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext()));
        ToggleVariableExpansionCommand = commandsManager.CreateRelayCommand<VariableSlot>(ToggleVariableExpansion, v => !IsWorking);
        //ShowDebugVariablesAsync();
    }

    [Conditional("DEBUG")]
    async void ShowDebugVariablesAsync()
    {
        var numericValue = new NumericVariableValue<ushort>(1234, ImmutableArray<byte>.Empty.Add(1).Add(2), 12);
        var numericSlot = new VariableSlot(
            new PdbVariable("Short1", 1, 2, Base: null, new PdbValueType(1, "ushort", 2, PdbVariableType.UInt16)), false);
        Items.Add(numericSlot);
        await Task.Delay(5000);
        numericSlot.Value = numericValue;
    }

    CancellationTokenSource? ctsUpdateForLineAsync;
    public void CancelUpdateForLine()
    {
        ctsUpdateForLineAsync?.Cancel();
        ClearVariables();
    }
    public async Task StartUpdateForLineAsync(PdbLine line)
    {
        ctsUpdateForLineAsync?.Cancel();
        ctsUpdateForLineAsync = new CancellationTokenSource();
        IsWorkingCount++;
        try
        {
            await UpdateForLineAsync(line, ctsUpdateForLineAsync.Token);
        }
        catch (OperationCanceledException)
        {
            // ignore cancellations
        }
        finally
        {
            IsWorkingCount--;
        }
    }
    void ToggleVariableExpansion(VariableSlot? variableSlot)
    {
        if (variableSlot is not null)
        {
            if (variableSlot.IsExpanded)
            {
                int slotPosition = Items.IndexOf(variableSlot);
                while (Items.Count > slotPosition && Items[slotPosition+1].Level > variableSlot.Level)
                {
                    Items.RemoveAt(slotPosition + 1);
                }
                variableSlot.IsExpanded = false;
            }
            else
            {
                int slotPosition = Items.IndexOf(variableSlot);
                int start = variableSlot.Source.Start;
                ushort? baseAddress = variableSlot.Source.Base;
                switch (variableSlot.Source.Type)
                {
                    case PdbArrayType arrayType:
                        var arrayVariableValue = (ArrayVariableValue?)variableSlot.Value!;
                        int arrayItemSize = arrayType.ReferencedOfType!.Size;
                        for (int i = 0; i < (arrayType.ItemsCount ?? 0); i++)
                        {
                            int end = start + arrayItemSize;
                            var arrayItemVariable = new PdbVariable($"[{i}]", start, end, baseAddress, arrayType.ReferencedOfType!);
                            var arrayItemSlot = new VariableSlot(arrayItemVariable, variableSlot.IsGlobal, variableSlot.Level + 1);
                            var arrayItemData = variableSlot.Data!.Value.AsSpan().Slice(i * arrayItemSize, arrayItemSize);
                            var variableValue = arrayVariableValue.Items[i];
                            if (variableValue is not null)
                            {
                                arrayItemSlot.Value = variableValue;
                            }
                            start += arrayItemSize;
                            Items.Insert(slotPosition + 1 + i, arrayItemSlot);
                        }
                        break;
                    case PdbStructType structType:
                        var structVariableValue = (StructVariableValue?)variableSlot.Value!;
                        for (int i=0; i<structType.Members.Length; i++)
                        {
                            var variableValue = structVariableValue.Items[i];
                            var member = structType.Members[i];
                            var memberType = (PdbDefinedType)member.Type;
                            int memberStart = start + member.Offset;
                            var memberVariable = new PdbVariable(
                                member.Name, 
                                memberStart, memberStart + variableValue.Data.Length,
                                baseAddress,
                                memberType);
                            var structMemberSlot = new VariableSlot(memberVariable, variableSlot.IsGlobal, variableSlot.Level + 1);
                            if (variableValue is not null)
                            {
                                structMemberSlot.Value = variableValue;
                            }
                            Items.Insert(slotPosition + 1 + i, structMemberSlot);
                        }
                        break;
                    case PdbPtrType ptrType:
                        var ptrVariableValue = (PtrVariableValue)variableSlot.Value!;
                        var referencedType = ptrType.ReferencedOfType!;
                        ushort endAddress = (ushort)(ptrVariableValue.PointerAddress + referencedType.Size);
                        var variable = new PdbVariable("*", ptrVariableValue.PointerAddress, endAddress, null, referencedType);
                        var referencedTypeSlot = new VariableSlot(variable, variableSlot.IsGlobal, variableSlot.Level + 1);
                        Items.Insert(slotPosition + 1, referencedTypeSlot);
                        ctsUpdateForLineAsync?.Cancel();
                        ctsUpdateForLineAsync = new ();
                        _ = FillVariableValueAsync(referencedTypeSlot, variable, ctsUpdateForLineAsync.Token);
                        break;

                }
                variableSlot.IsExpanded = true;
            }
        }
    }
    /// <summary>
    /// Creates slots for both function and global variables. Values are fetched asynchronously.
    /// </summary>
    /// <param name="line"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task UpdateForLineAsync(PdbLine line, CancellationToken ct = default)
    {
        ClearVariables();
        var functionVariables = line.Function?.Variables ?? ImmutableDictionary<string, PdbVariable>.Empty;
        var globalVariables = globals.Project?.DebugSymbols?.GlobalVariables ?? ImmutableDictionary<string, PdbVariable>.Empty;
        if (!functionVariables.IsEmpty || !globalVariables.IsEmpty)
        {
            var mapBuilder = ImmutableDictionary.CreateBuilder<PdbVariable, VariableSlot>();
            foreach (var variable in functionVariables.Values)
            {
                var slot = new VariableSlot(variable, isGlobal: false);
                Items.Add(slot);
                mapBuilder.Add(variable, slot);
            }
            foreach (var variable in globalVariables)
            {
                var slot = new VariableSlot(variable.Value, isGlobal: true);
                Items.Add(slot);
                mapBuilder.Add(variable.Value, slot);
            }
            var map = mapBuilder.ToImmutable();
            await FillVariableValuesAsync(map, ct);
        }
    }
    internal async Task<ushort> GetVariableBaseAddressAsync(ushort basePointerAddress, CancellationToken ct)
    {
        var command = viceBridge.EnqueueCommand(
        new MemoryGetCommand(0, basePointerAddress, (ushort)(basePointerAddress + 1), MemSpace.MainMemory, 0));
        var response = await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command, ct: ct);
        using (var buffer = response?.Memory ?? throw new Exception("Failed to retrieve base address"))
        {
            ushort baseAddress = BitConverter.ToUInt16(buffer.Data);
            return baseAddress;
        }
    }
    /// <summary>
    /// Fetches memory required for variables and passes it to <see cref="FillVariableValue"/> that extracts it to
    /// variable values.
    /// </summary>
    /// <param name="map"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    async Task FillVariableValuesAsync(ImmutableDictionary<PdbVariable, VariableSlot> map, CancellationToken ct)
    {
        foreach (var variable in map.Keys)
        {
            await FillVariableValueAsync(map[variable], variable, ct);
        }
    }

    async Task FillVariableValueAsync(VariableSlot slot, PdbVariable variable, CancellationToken ct)
    {
        ushort memoryStart;
        ushort memoryEnd;
        if (variable.Base is not null)
        {
            ushort baseAddress = await GetVariableBaseAddressAsync(variable.Base.Value, ct);
            memoryStart = (ushort)(baseAddress + variable.Start);
            memoryEnd = (ushort)(baseAddress + variable.End);
        }
        else
        {
            memoryStart = (ushort)variable.Start;
            memoryEnd = (ushort)variable.End;
        }
        var command = viceBridge.EnqueueCommand(
            new MemoryGetCommand(0, memoryStart, memoryEnd, MemSpace.MainMemory, 0));
        var response = await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command, ct: ct);
        ct.ThrowIfCancellationRequested();
        var buffer = response?.Memory;
        if (buffer is not null)
        {
            var data = buffer.Value.Data;
            try
            {
                FillVariableValue(slot, variable, data);
            }
            finally
            {
                buffer.Value.Dispose();
            }
        }
    }

    void FillVariableValue(VariableSlot slot, PdbVariable variable, ReadOnlySpan<byte> data)
    {
        var variableValue = CreateVariableValue(variable.Type, (ushort)variable.Start, data);
        if (variableValue is not null)
        {
            slot.Value = variableValue;
        }
    }

    internal VariableValue? CreateVariableValue(PdbDefinedType type, ushort start, ReadOnlySpan<byte> buffer)
    {
        switch (type)
        {
            case PdbValueType valueType:
                var numericValue = CreateNumericVariable(valueType, start, buffer);
                return numericValue;
            case PdbPtrType ptrType:
                var ptrValue = new PtrVariableValue(start, buffer.Copy(2),
                    BitConverter.ToUInt16(buffer));
                return ptrValue;
            case PdbArrayType arrayType:
                {
                    var builder = ImmutableArray.CreateBuilder<VariableValue>();
                    if (arrayType.ItemsCount > 0)
                    {
                        int step = arrayType.ReferencedOfType!.Size;
                        ushort itemRelativeStart = 0;
                        for (int i = 0; i < arrayType.ItemsCount; i++)
                        {
                            var arrayBuffer = buffer.Slice(itemRelativeStart, step);
                            var itemVariableValue = CreateVariableValue(arrayType.ReferencedOfType!,
                                (ushort)(itemRelativeStart + start), arrayBuffer);
                            if (itemVariableValue is not null)
                            {
                                builder.Add(itemVariableValue);
                            }
                            itemRelativeStart = (ushort)(itemRelativeStart + step);
                        }
                    }
                    var items = builder.ToImmutable();
                    var arrayValue = new ArrayVariableValue(start, buffer.Copy(type.Size), items, arrayType.ReferencedOfType!);
                    return arrayValue;
                }
            case PdbStructType structType:
                {
                    var builder = ImmutableArray.CreateBuilder<VariableValue>();
                    var members = structType.Members.Where(m => m.Type is PdbDefinedType).ToImmutableArray();
                    foreach (var member in members)
                    {
                        var memberType = (PdbDefinedType)member.Type;
                        var memberBuffer = buffer.Slice(member.Offset, memberType.Size);
                        var itemVariableValue = CreateVariableValue(memberType, (ushort)(member.Offset + start), memberBuffer);
                        if (itemVariableValue is not null)
                        {
                            builder.Add(itemVariableValue);
                        }
                    }
                    var items = builder.ToImmutable();
                    var structValue = new StructVariableValue(start, buffer.Copy(type.Size), items);
                    return structValue;
                }
        }
        return null;
    }

    internal VariableValue? CreateNumericVariable(PdbValueType valueType, ushort address, ReadOnlySpan<byte> buffer)
    {
        return valueType.VariableType switch
        {
            PdbVariableType.Bool => new NumericVariableValue<bool>(address, buffer.Copy(1), buffer[0] != 0),
            PdbVariableType.Byte => new NumericVariableValue<sbyte>(address, buffer.Copy(1), (sbyte)buffer[0]),
            PdbVariableType.UByte => new NumericVariableValue<byte>(address, buffer.Copy(1), buffer[0]),
            PdbVariableType.Int16 => new NumericVariableValue<short>(address, buffer.Copy(2), BitConverter.ToInt16(buffer)),
            PdbVariableType.UInt16 => new NumericVariableValue<ushort>(address, buffer.Copy(2), BitConverter.ToUInt16(buffer)),
            PdbVariableType.Int32 => new NumericVariableValue<int>(address, buffer.Copy(4), BitConverter.ToInt32(buffer)),
            PdbVariableType.UInt32 => new NumericVariableValue<uint>(address, buffer.Copy(4), BitConverter.ToUInt32(buffer)),
            PdbVariableType.Float => new NumericVariableValue<float>(address, buffer.Copy(4), BitConverter.ToSingle(buffer)),
            _ => null,
        };
    }
    public void ClearVariables()
    {
        Items.Clear();
    }
}

public class VariableSlot: NotifiableObject
{
    public bool IsExpandable => Source.Type is PdbStructType || Source.Type is PdbArrayType 
        || (Source.Type is PdbPtrType ptrType && ptrType.ReferencedOfType is not null);
    public bool IsExpanded { get; set; }
    public PdbVariable Source { get; }
    public string Name => Source.Name;
    public int Size => Source.Type.Size;
    public ushort? Base => Source.Base;
    public VariableValue? Value { get; internal set; }
    public bool HasValue => Value is not null;
    public bool IsDefaultRepresentation => HasValue && !IsHexRepresentation;
    public bool IsHexRepresentation => HasValue && Source.Type is PdbPtrType;
    public bool CanBeChar => HasValue && Source.Type is PdbValueType valueType && valueType.VariableType == PdbVariableType.UByte;
    public bool IsGlobal { get; }
    public  int Level { get; }
    public VariableSlot(PdbVariable source, bool isGlobal, int level = 0)
    {
        Source = source;
        IsGlobal = isGlobal;
        Level = level;
    }
    public object? VariableValue => Value?.CoreValue;
    public ushort? Address => Value?.Address;
    public ImmutableArray<byte>? Data => Value?.Data;
    public string ValueType => Source.Type.ValueType;
}

public abstract class VariableValue
{
    public ImmutableArray<byte> Data { get; }
    public ushort Address { get; }
    public VariableValue(ushort address, ImmutableArray<byte> data)
    {
        Address = address;
        Data = data;
    }
    public abstract object? CoreValue { get; }
    
}

public class NumericVariableValue<T>: VariableValue
    where T: struct
{
    public T Value { get; }

    public override object? CoreValue => Value;

    public NumericVariableValue(ushort address, ImmutableArray<byte> data, T value) 
        : base(address, data)
    {
        Value = value;
    }
}

public class PtrVariableValue : VariableValue
{
    public ushort PointerAddress { get; }
    public override object? CoreValue => PointerAddress;
    public PtrVariableValue(ushort address, ImmutableArray<byte> data, ushort pointerAddress) : base(address, data)
    {
        PointerAddress = pointerAddress;
    }
}
public class StructVariableValue: VariableValue
{
    public ImmutableArray<VariableValue> Items { get; }
    public override object? CoreValue => null;
    public StructVariableValue(ushort address, ImmutableArray<byte> data, ImmutableArray<VariableValue> items) 
        : base(address, data)
    {
        Items = items;
    }
}
public  class ArrayVariableValue: VariableValue
{
    public ImmutableArray<VariableValue> Items { get; }
    public PdbType ItemType { get; }
    public override object? CoreValue
    {
        get
        {
            switch (ItemType)
            {
                case PdbValueType valueType:
                    int allItems = Data.Length / valueType.Size;
                    int count = Math.Min(4, allItems);
                    var texts = new List<string?>(count);
                    for (int i = 0; i < count; i++)
                    {
                        var data = Data.AsSpan().Slice(i * valueType.Size, valueType.Size);
                        texts.Add(ConvertValue(valueType.VariableType, data)?.ToString());
                    }
                    if (allItems > count)
                    {
                        texts.Add("...");
                    }
                    string content = string.Join(", ", texts);
                    return $"[{content}]";
                default:
                    return $"{Items.Length} items";
            }
        }
    }
    internal object? ConvertValue(PdbVariableType variableType, ReadOnlySpan<byte> buffer)
    {
        return variableType switch
        {
            PdbVariableType.Bool => buffer[0] != 0,
            PdbVariableType.Byte => (sbyte)buffer[0],
            PdbVariableType.UByte => buffer[0],
            PdbVariableType.Int16 => BitConverter.ToInt16(buffer),
            PdbVariableType.UInt16 => BitConverter.ToUInt16(buffer),
            PdbVariableType.Int32 => BitConverter.ToInt32(buffer),
            PdbVariableType.UInt32 => BitConverter.ToUInt32(buffer),
            PdbVariableType.Float => BitConverter.ToSingle(buffer),
            _ => null,
        };
    }

    public ArrayVariableValue(ushort address, ImmutableArray<byte> data, 
        ImmutableArray<VariableValue> items, PdbType itemType) 
        : base(address, data)
    {
        Items = items;
        ItemType = itemType;
    }
}
