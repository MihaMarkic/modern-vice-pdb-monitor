using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public class CallStackViewModel: NotifiableObject
{
    readonly ILogger<CallStackViewModel> logger;
    readonly IDispatcher dispatcher;
    readonly Globals globals;
    readonly EmulatorMemoryViewModel emulatorMemoryViewModel;
    readonly RegistersViewModel registersViewModel;
    readonly ExecutionStatusViewModel executionStatusViewModel;
    public RelayCommand<SourceCallStackItem> GoToLineCommand { get; }
    public ImmutableArray<CallStackItem> CallStack { get; private set; }
    public CallStackViewModel(ILogger<CallStackViewModel> logger, IDispatcher dispatcher, Globals globals, 
        EmulatorMemoryViewModel emulatorMemoryViewModel, RegistersViewModel registersViewModel,
        ExecutionStatusViewModel executionStatusViewModel)
    {
        this.logger = logger;
        this.dispatcher = dispatcher;
        this.globals = globals;
        this.emulatorMemoryViewModel = emulatorMemoryViewModel;
        this.registersViewModel = registersViewModel;
        this.executionStatusViewModel = executionStatusViewModel;
        CallStack = ImmutableArray<CallStackItem>.Empty;
        registersViewModel.RegistersUpdated += RegistersViewModel_RegistersUpdated;
        emulatorMemoryViewModel.MemoryContentChanged += EmulatorMemoryViewModel_MemoryContentChanged;
        executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
        GoToLineCommand = new RelayCommand<SourceCallStackItem>(GoToLine, i => i is SourceCallStackItem);
    }
    private void Clear()
    {
        CallStack = ImmutableArray<CallStackItem>.Empty;
    }
    private void GoToLine(SourceCallStackItem? e)
    {
        if (e is not null)
        {
            dispatcher.Dispatch(new OpenSourceFileMessage(e.File, e.Line.LineNumber+1, Column: 0, MoveCaret: true));
        }
    }
    private void ExecutionStatusViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ExecutionStatusViewModel.IsDebuggingPaused):
                if (!executionStatusViewModel.IsDebuggingPaused)
                {
                    Clear();
                }
                break;
            case nameof(ExecutionStatusViewModel.IsDebugging):
                if (!executionStatusViewModel.IsDebugging)
                {
                    Clear();
                }
                break;
        }
    }

    private bool IsPaused => executionStatusViewModel.IsDebugging && executionStatusViewModel.IsDebuggingPaused;

    private void RegistersViewModel_RegistersUpdated(object? sender, EventArgs e)
    {
        if (IsPaused && registersViewModel.Current.SP.HasValue)
        {
            CreateCallStack(emulatorMemoryViewModel.Current, registersViewModel.Current.SP.Value);
        }
    }

    private void EmulatorMemoryViewModel_MemoryContentChanged(object? sender, EventArgs e)
    {
        if (IsPaused && registersViewModel.Current.SP.HasValue)
        {
            CreateCallStack(emulatorMemoryViewModel.Current, registersViewModel.Current.SP.Value);
        }
    }

    internal void CreateCallStack(ReadOnlySpan<byte> memory, byte sp)
    {
        var builder = ImmutableArray.CreateBuilder<CallStackItem>();
        // 0xF4 is main function entry SP
        byte i = 0xF4 - 2;
        while (i >= sp)
        {
            ushort spAddress = (ushort)(0x0100 + i);
            ushort memAddress = BitConverter.ToUInt16([memory[spAddress+1], memory[spAddress + 2]]);
            if (memAddress >= 2)
            {
                ushort sourceAddress = (ushort)(memAddress - 2);
                // check if instruction could be JSR
                if (IsValidCall(memory, sourceAddress))
                {
                    var match = FindMatchingSourceLine(sourceAddress);
                    if (match is null)
                    {
                        builder.Add(new UnknownCallStackItem(sourceAddress));
                    }
                    else
                    {
                        builder.Add(new SourceCallStackItem(
                            sourceAddress, 
                            match.Value.File, 
                            match.Value.Function,
                            match.Value.Line, 
                            match.Value.AssemblyLine));
                    }
                    i -= 2;
                }
                else
                {
                    i--;
                }
            }
            else
            {
                logger.LogError("StackPointer is pointing to sub zero memory address {Address}", memAddress - 2);
                i--;
            }
        }
        CallStack = builder.ToImmutable();
    }

    internal bool IsValidCall(ReadOnlySpan<byte> memory, ushort sourceAddress)
    {
        // for now it just checks whether JSR was at the calling address
        return memory[sourceAddress] == 0x20;
    }

    internal (PdbFile File, PdbFunction Function, PdbLine Line, PdbAssemblyLine? AssemblyLine)? FindMatchingSourceLine(ushort address)
    {
        var debugSymbols = globals.Project?.DebugSymbols;
        var addressToLineMap = debugSymbols?.AddressToLineMap;
        if (addressToLineMap is not null)
        {
            var segmentItem = addressToLineMap.FindLineAtAddress(address);
            if (segmentItem is not null)
            {
                PdbFile file = debugSymbols!.LinesToFilesMap[segmentItem.Line];
                PdbFunction function = segmentItem.Line.Function.ValueOrThrow();
                return (file, function, segmentItem.Line, segmentItem.AssemblyLine);
            }
        }
        else
        {
            logger.LogError("AddressToLineMap was null");
        }
        return null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            registersViewModel.RegistersUpdated -= RegistersViewModel_RegistersUpdated;
            emulatorMemoryViewModel.MemoryContentChanged -= EmulatorMemoryViewModel_MemoryContentChanged;
            executionStatusViewModel.PropertyChanged -= ExecutionStatusViewModel_PropertyChanged;
        }
        base.Dispose(disposing);
    }

    public abstract record CallStackItem(ushort Address, int? LineNumber, string FileText, string FunctionText, string LineText, string Assembly);
    public record SourceCallStackItem(ushort Address,
        PdbFile File, PdbFunction Function, PdbLine Line, PdbAssemblyLine? AssemblyLine)
        : CallStackItem(Address, Line.LineNumber + 1, File.Path.Path, Function.XName, Line.Text.Trim(), AssemblyLine?.Text.Trim() ?? string.Empty);
    public record UnknownCallStackItem(ushort Address) : CallStackItem(Address, null, string.Empty, string.Empty, "[External code]", string.Empty);
}
