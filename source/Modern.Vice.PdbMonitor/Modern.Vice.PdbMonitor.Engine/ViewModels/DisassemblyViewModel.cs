using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Models.OpCodes;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;
public class DisassemblyViewModel : ScopedViewModel, IViewableContent
{
    readonly ILogger<DisassemblyViewModel> logger;
    readonly IDisassembler disassembler;
    readonly EmulatorMemoryViewModel emulatorMemoryViewModel;
    public ushort Address { get; private set; }
    public string Caption => $"{Address:x4}";
    public string? AddressForJump { get; set; }
    public RelayCommand JumpToAddressCommand { get; }
    readonly List<DisassemblyLine> lines = new ();
    public DisassemblyViewModel(ILogger<DisassemblyViewModel> logger, IDisassembler disassembler,
        EmulatorMemoryViewModel emulatorMemoryViewModel,
        ushort address)
    {
        this.logger = logger;
        this.disassembler = disassembler;
        this.emulatorMemoryViewModel = emulatorMemoryViewModel;
        Address = address;
        JumpToAddressCommand = new RelayCommand(JumpTo, () => IsAddressForJumpValid);
    }
    internal bool IsAddressForJumpValid => ushort.TryParse(AddressForJump, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
    internal void JumpTo()
    {
        Address = ushort.Parse(AddressForJump.ValueOrThrow(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        lines.Clear();
        OnPropertyChanged(nameof(Lines));
    }
    public void ClearExecutionRow()
    {
    }
    /// <summary>
    /// Provides cached on demand access to disassembled lines.
    /// </summary>
    public IEnumerable<DisassemblyLine> Lines
    {
        get
        {
            if (lines.Count > 0)
            {
                foreach (var line in lines)
                {
                    yield return line;
                }
            }
            else
            {
                int i = 0;
                ushort start = Address;
                bool isValid = true;
                var memory = emulatorMemoryViewModel.Current[Address..];
                do
                {
                    if (lines.Count > i)
                    {
                        yield return lines[i];
                    }
                    else
                    {
                        var instruction = disassembler.DisassembleInstruction(memory.Span);
                        if (instruction is not null)
                        {
                            memory = memory[instruction.Mode.Length..];
                            var line = new DisassemblyLine(start, instruction, isExecuting: false);
                            start += (ushort)instruction.Mode.Length;
                            lines.Add(line);
                            yield return line;
                        }
                        else
                        {
                            isValid = false;
                        }
                    }
                    i++;
                } while (isValid);
            }
        }
    }
    protected override void OnPropertyChanged([CallerMemberName] string name = default!)
    {
        base.OnPropertyChanged(name);
        switch (name)
        {
            case nameof(AddressForJump):
                JumpToAddressCommand.RaiseCanExecuteChanged();
                break;
        }
    }
}

public class DisassemblyLine: NotifiableObject
{
    readonly Instruction instruction;
    public bool IsExecuting { get; set; }
    public byte OpCode => instruction.Mode.OpCode;
    public InstructionName OpCodeName => instruction.Name;
    public InstructionMode Mode => instruction.Mode;
    public ushort Address { get; }
    public DisassemblyLine(ushort address, Instruction instruction, bool isExecuting)
    {
        Address = address;
        this.instruction = instruction;
        IsExecuting = isExecuting;
    }
}

public class DesignDisassemblyLine: DisassemblyLine
{
    public DesignDisassemblyLine() 
        : base(
            0x1AE0, 
            new Instruction(InstructionName.ADC, new AbsoluteInstructionMode(0x6D, 0x10, 0x99, 4)),
            isExecuting: true)
    {

    }
}
