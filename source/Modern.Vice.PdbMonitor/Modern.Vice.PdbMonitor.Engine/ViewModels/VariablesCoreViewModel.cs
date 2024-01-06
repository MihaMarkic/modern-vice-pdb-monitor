using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Extensions;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public abstract class VariablesCoreViewModel : NotifiableObject
{
    readonly ILogger logger;
    readonly IViceBridge viceBridge;
    readonly IDispatcher dispatcher;
    readonly EmulatorMemoryViewModel emulatorMemoryViewModel;
    protected readonly ExecutionStatusViewModel executionStatusViewModel;
    protected readonly Globals globals;
    readonly CommandsManager commandsManager;
    public RelayCommand<VariableSlot> ToggleVariableExpansionCommand { get; }
    public RelayCommand<VariableSlot> RemoveVariableCommand { get; }
    public RelayCommand RemoveAllCommand { get; }
    public ObservableCollection<VariableSlot> Items { get; } = new ObservableCollection<VariableSlot>();
    public bool IsWorking => IsWorkingCount > 0;
    internal bool IsDebuggingPaused => executionStatusViewModel.IsDebuggingPaused;
    int IsWorkingCount { get; set; }
    public VariablesCoreViewModel(ILogger logger, IViceBridge viceBridge,
        IDispatcher dispatcher, EmulatorMemoryViewModel emulatorMemoryViewModel,
        ExecutionStatusViewModel executionStatusViewModel,
        Globals globals)
    {
        this.logger = logger;
        this.viceBridge = viceBridge;
        this.dispatcher = dispatcher;
        this.emulatorMemoryViewModel = emulatorMemoryViewModel;
        this.executionStatusViewModel = executionStatusViewModel;
        this.globals = globals;
        executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
        commandsManager = new CommandsManager(this, new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext()));
        ToggleVariableExpansionCommand = commandsManager.CreateRelayCommand<VariableSlot>(ToggleVariableExpansion,
            v => !IsWorking && IsDebuggingPaused);
        RemoveVariableCommand = new RelayCommand<VariableSlot>(RemoveVariable, v => v is not null);
        RemoveAllCommand = new RelayCommand(RemoveAll);
        //ShowDebugVariablesAsync();
    }

    private void ExecutionStatusViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IsDebuggingPaused):
                OnPropertyChanged(nameof(IsDebuggingPaused));
                break;
        }
    }

    [Conditional("DEBUG")]
    async void ShowDebugVariablesAsync()
    {
        var numericValue = new NumericVariableValue<ushort>(1234, ImmutableArray<byte>.Empty.Add(1).Add(2), 12);
        var numericSlot = new VariableSlot(
            new PdbVariable("Short1", 1, 2, Base: null,
                new PdbValueType(1, "ushort", 2, PdbVariableType.UInt16),
                new SymbolDeclarationSource(PdbPath.Create("xxx", "xxx"), 0, 0)),
            false);
        Items.Add(numericSlot);
        await Task.Delay(5000);
        numericSlot.Value = numericValue;
    }
    public VariableSlot? GetVariableSlot(PdbVariable variable, bool isGlobal)
    {
        if (executionStatusViewModel.IsDebuggingPaused)
        {
            var slot = new VariableSlot(variable, isGlobal: isGlobal);
            FillVariableValue(slot, variable);
            return slot;
        }
        return null;
    }
    void RemoveVariable(VariableSlot? source)
    {
        if (source is not null)
        {
            Items.Remove(source);
        }
    }

    void RemoveAll()
    {
        Items.Clear();
    }

    void ToggleVariableExpansion(VariableSlot? variableSlot)
    {
        if (variableSlot is not null)
        {
            if (variableSlot.IsExpanded)
            {
                int slotPosition = Items.IndexOf(variableSlot);
                while (Items.Count > slotPosition + 1 && Items[slotPosition + 1].Level > variableSlot.Level)
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
                            var arrayItemVariable = new PdbVariable($"[{i}]", start, end, baseAddress, arrayType.ReferencedOfType!, null);
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
                        var sortedMembers = structType.Members.OrderBy(m => m.Name).ToImmutableArray();
                        for (int i = 0; i < sortedMembers.Length; i++)
                        {
                            var variableValue = structVariableValue.Items[i];
                            var member = structType.Members[i];
                            var memberType = (PdbDefinedType)member.Type;
                            int memberStart = start + member.Offset;
                            var memberVariable = new PdbVariable(
                                member.Name,
                                memberStart, memberStart + variableValue.Data.Length,
                                baseAddress,
                                memberType,
                                null);
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
                        var variable = new PdbVariable("*", ptrVariableValue.PointerAddress, endAddress, null, referencedType, null);
                        var referencedTypeSlot = new VariableSlot(variable, variableSlot.IsGlobal, variableSlot.Level + 1);
                        Items.Insert(slotPosition + 1, referencedTypeSlot);
                        FillVariableValue(referencedTypeSlot, variable);
                        break;
                }
                variableSlot.IsExpanded = true;
            }
        }
    }

    internal ushort GetVariableBaseAddress(ushort basePointerAddress) => emulatorMemoryViewModel.GetShortAt(basePointerAddress);
    //var command = viceBridge.EnqueueCommand(
    //new MemoryGetCommand(0, basePointerAddress, (ushort)(basePointerAddress + 1), MemSpace.MainMemory, 0),
    //    true);
    //var response = await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command, ct: ct);
    //using (var buffer = response?.Memory ?? throw new Exception("Failed to retrieve base address"))
    //{
    //    ushort baseAddress = BitConverter.ToUInt16(buffer.Data);
    //    return baseAddress;
    //}
    /// <summary>
    /// Fetches memory required for variables and passes it to <see cref="FillVariableValue"/> that extracts it to
    /// variable values.
    /// </summary>
    /// <param name="map"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    protected void FillVariableValues(ImmutableDictionary<PdbVariable, VariableSlot> map)
    {
        foreach (var variable in map.Keys)
        {
            FillVariableValue(map[variable], variable);
        }
    }

    protected void FillVariableValue(VariableSlot slot, PdbVariable variable)
    {
        // don't try to fill when not running
        if (executionStatusViewModel.IsDebuggingPaused)
        {
            ushort memoryStart;
            ushort memoryEnd;
            if (variable.Base is not null)
            {
                ushort baseAddress = GetVariableBaseAddress(variable.Base.Value);
                memoryStart = (ushort)(baseAddress + variable.Start);
                memoryEnd = (ushort)(baseAddress + variable.End);
            }
            else
            {
                memoryStart = (ushort)variable.Start;
                memoryEnd = (ushort)variable.End;
            }
            var data = emulatorMemoryViewModel.GetSpan(memoryStart, memoryEnd);
            FillVariableValue(slot, variable, data);
        }
        //var command = viceBridge.EnqueueCommand(
        //    new MemoryGetCommand(0, memoryStart, memoryEnd, MemSpace.MainMemory, 0),
        //    resumeOnStopped: true);
        //var response = await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command, ct: ct);
        //ct.ThrowIfCancellationRequested();
        //var buffer = response?.Memory;
        //if (buffer is not null)
        //{
        //    var data = buffer.Value.Data;
        //    try
        //    {
        //        FillVariableValue(slot, variable, data);
        //    }
        //    finally
        //    {
        //        buffer.Value.Dispose();
        //    }
        //}
    }

    public async Task UpdateVariableValueAsync(VariableSlot slot, object value, CancellationToken ct)
    {
        if (slot.Value is not null)
        {
            byte[] bytes;
            object correctedValue;
            if (slot.Source.Type is PdbEnumType enumType)
            {
                correctedValue = enumType.VariableType switch
                {
                    PdbVariableType.UByte => (object)Convert.ToByte(value),
                    PdbVariableType.UInt16 => (object)Convert.ToUInt16(value),
                    PdbVariableType.UInt32 => (object)Convert.ToUInt32(value),
                    _ => throw new ArgumentException($"Type {enumType.VariableType} is not supported"),
                };
                bytes = enumType.VariableType switch
                {
                    PdbVariableType.UByte => new byte[] { (byte)correctedValue },
                    PdbVariableType.UInt16 => BitConverter.GetBytes((UInt16)correctedValue),
                    PdbVariableType.UInt32 => BitConverter.GetBytes((UInt32)correctedValue),
                    _ => throw new ArgumentException($"Type {enumType.VariableType} is not supported"),
                };
            }
            else if (slot.Source.Type is PdbValueType valueType)
            {
                bytes = GetValueBytes(valueType.VariableType, value);
                correctedValue = value;
            }
            else
            {
                throw new ArgumentException($"Type {slot.Source.Type.GetType().Name} is not supported");
            }

            using (var buffer = BufferManager.GetBuffer((uint)bytes.Length))
            {
                Buffer.BlockCopy(bytes, 0, buffer.Data, 0, bytes.Length);
                var command = viceBridge.EnqueueCommand(new MemorySetCommand(0, slot.Value.Address, MemSpace.MainMemory, 0, buffer));
                var response = await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command, ct: ct);
                if (response?.ErrorCode == ErrorCode.OK)
                {
                    emulatorMemoryViewModel.UpdateMemory(slot.Value.Address, bytes.AsSpan());
                    slot.Value = CreateNumericVariable((PdbValueType)slot.Source.Type, slot.Value.Address, bytes.AsSpan());
                }
                else
                {
                    logger.LogError("Failed updating variable at address {Address}", slot.Value.Address.ToString("X4"));
                }
            }
        }
    }

    internal byte[] GetValueBytes(PdbVariableType variableType, object value)
    {
        return variableType switch
        {
            PdbVariableType.Bool => BitConverter.GetBytes((bool)value),
            PdbVariableType.Byte => new byte[] { unchecked((byte)value) },
            PdbVariableType.UByte => new byte[] { (byte)value },
            PdbVariableType.Int16 => BitConverter.GetBytes((Int16)value),
            PdbVariableType.UInt16 => BitConverter.GetBytes((UInt16)value),
            PdbVariableType.Int32 => BitConverter.GetBytes((Int32)value),
            PdbVariableType.UInt32 => BitConverter.GetBytes((UInt32)value),
            PdbVariableType.Float => BitConverter.GetBytes((float)value),
            _ => throw new ArgumentException($"Type {variableType} is not supported"),
        };
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            executionStatusViewModel.PropertyChanged -= ExecutionStatusViewModel_PropertyChanged;
        }
        base.Dispose(disposing);
    }
}

public class VariableSlot : NotifiableObject
{
    public bool IsExpandable => Source.Type is PdbStructType || Source.Type is PdbArrayType
        || (Source.Type is PdbPtrType ptrType && ptrType.ReferencedOfType is not null);
    public bool IsExpanded { get; set; }
    public PdbVariable Source { get; }
    public string Name => Source.Name;
    public int Size => Source.Type.Size;
    public ushort? Base => Source.Base;
    public VariableValue? Value { get; set; }
    public bool HasValue => Value is not null;
    public bool IsDefaultRepresentation => HasValue && !IsHexRepresentation;
    public bool IsHexRepresentation => HasValue && Source.Type is PdbPtrType;
    public bool CanBeChar => HasValue
        && Source.Type is PdbValueType valueType
        && valueType.VariableType == PdbVariableType.UByte
        && !IsEnum;
    public bool IsGlobal { get; }
    public int Level { get; }
    public bool IsEnum { get; }
    public string? EnumValue { get; internal set; }
    public bool IsEditable => Source.Type is PdbValueType;
    readonly PdbEnumType? enumType;
    public VariableSlot(PdbVariable source, bool isGlobal, int level = 0)
    {
        Source = source;
        IsGlobal = isGlobal;
        Level = level;
        if (source.Type is PdbEnumType et)
        {
            IsEnum = true;
            enumType = et;
        }
    }
    public object? VariableValue => Value?.CoreValue;
    public ushort? Address => Value?.Address;
    public ImmutableArray<byte>? Data => Value?.Data;
    public string ValueType => IsEnum ? $"enum({Source.Type.ValueType})" : Source.Type.ValueType;
    protected override void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        switch (name)
        {
            case nameof(Value):
                if (IsEnum && enumType is not null && Value?.CoreValue is not null)
                {
                    uint key = Convert.ToUInt32(Value.CoreValue);
                    EnumValue = enumType.ByKey.TryGetValue(key, out string? text) ? text : string.Empty;
                }
                break;
        }
        base.OnPropertyChanged(name!);
    }
    public void ClearValue()
    {
        Value = null;
    }
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

public class NumericVariableValue<T> : VariableValue
    where T : struct
{
    public T Value { get; private set; }

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
public class StructVariableValue : VariableValue
{
    public ImmutableArray<VariableValue> Items { get; }
    public override object? CoreValue => null;
    public StructVariableValue(ushort address, ImmutableArray<byte> data, ImmutableArray<VariableValue> items)
        : base(address, data)
    {
        Items = items;
    }
}
public class ArrayVariableValue : VariableValue
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
