﻿using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Engine.Models.OpCodes;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation;
public class Disassembler : IDisassembler
{
    readonly ILogger<Disassembler> logger;
    public Disassembler(ILogger<Disassembler> logger)
    {
        this.logger = logger;
    }

    public ImmutableArray<Instruction> Disassemble(ReadOnlySpan<byte> data)
    {
        var builder = ImmutableArray.CreateBuilder<Instruction>();
        Instruction? instruction;
        do
        {
            instruction = DisassembleInstruction(data);
            if (instruction is not null)
            {
                builder.Add(instruction);
                data = data.Slice(instruction.Mode.Length + 1);
            }
        } while (instruction is not null);
        return builder.ToImmutable();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    /// <remarks>Autogenerated code using LINQ scripts.</remarks>
    public Instruction? DisassembleInstruction(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
        {
            return null;
        }
        return data[0] switch
        {
            // ADC
            0x69 => new Instruction(InstructionName.ADC, new ImmediateInstructionMode(0x69, data[1], 2)),
            0x65 => new Instruction(InstructionName.ADC, new ZeroPageInstructionMode(0x65, data[1], 3)),
            0x75 => new Instruction(InstructionName.ADC, new ZeroPageXInstructionMode(0x75, data[1], 4)),
            0x6D => new Instruction(InstructionName.ADC, new AbsoluteInstructionMode(0x6D, data[1], data[2], 4)),
            0x7D => new Instruction(InstructionName.ADC, new AbsoluteXInstructionMode(0x7D, data[1], data[2], 4)),
            0x79 => new Instruction(InstructionName.ADC, new AbsoluteYInstructionMode(0x79, data[1], data[2], 4)),
            0x61 => new Instruction(InstructionName.ADC, new IndirectXInstructionMode(0x61, data[1], 6)),
            0x71 => new Instruction(InstructionName.ADC, new IndirectYInstructionMode(0x71, data[1], 5)),
            // AND
            0x29 => new Instruction(InstructionName.AND, new ImmediateInstructionMode(0x29, data[1], 2)),
            0x25 => new Instruction(InstructionName.AND, new ZeroPageInstructionMode(0x25, data[1], 3)),
            0x35 => new Instruction(InstructionName.AND, new ZeroPageXInstructionMode(0x35, data[1], 4)),
            0x2D => new Instruction(InstructionName.AND, new AbsoluteInstructionMode(0x2D, data[1], data[2], 4)),
            0x3D => new Instruction(InstructionName.AND, new AbsoluteXInstructionMode(0x3D, data[1], data[2], 4)),
            0x39 => new Instruction(InstructionName.AND, new AbsoluteYInstructionMode(0x39, data[1], data[2], 4)),
            0x21 => new Instruction(InstructionName.AND, new IndirectXInstructionMode(0x21, data[1], 6)),
            0x31 => new Instruction(InstructionName.AND, new IndirectYInstructionMode(0x31, data[1], 5)),
            // ASL
            0x0A => new Instruction(InstructionName.ASL, new AccumulatorInstructionMode(0x0A, 2)),
            0x06 => new Instruction(InstructionName.ASL, new ZeroPageInstructionMode(0x06, data[1], 5)),
            0x16 => new Instruction(InstructionName.ASL, new ZeroPageXInstructionMode(0x16, data[1], 6)),
            0x0E => new Instruction(InstructionName.ASL, new AbsoluteInstructionMode(0x0E, data[1], data[2], 6)),
            0x1E => new Instruction(InstructionName.ASL, new AbsoluteXInstructionMode(0x1E, data[1], data[2], 7)),
            // BCC
            0x90 => new Instruction(InstructionName.BCC, new RelativeInstructionMode(0x90, (sbyte)data[1], 2)),
            // BCS
            0xB0 => new Instruction(InstructionName.BCS, new RelativeInstructionMode(0xB0, (sbyte)data[1], 2)),
            // BEQ
            0xF0 => new Instruction(InstructionName.BEQ, new RelativeInstructionMode(0xF0, (sbyte)data[1], 2)),
            // BIT
            0x24 => new Instruction(InstructionName.BIT, new ZeroPageInstructionMode(0x24, data[1], 3)),
            0x2C => new Instruction(InstructionName.BIT, new AbsoluteInstructionMode(0x2C, data[1], data[2], 4)),
            // BMI
            0x30 => new Instruction(InstructionName.BMI, new RelativeInstructionMode(0x30, (sbyte)data[1], 2)),
            // BNE
            0xD0 => new Instruction(InstructionName.BNE, new RelativeInstructionMode(0xD0, (sbyte)data[1], 2)),
            // BPL
            0x10 => new Instruction(InstructionName.BPL, new RelativeInstructionMode(0x10, (sbyte)data[1], 2)),
            // BRK
            0x00 => new Instruction(InstructionName.BRK, new ImpliedInstructionMode(0x00, 7)),
            // BVC
            0x50 => new Instruction(InstructionName.BVC, new RelativeInstructionMode(0x50, (sbyte)data[1], 2)),
            // BVS
            0x70 => new Instruction(InstructionName.BVS, new RelativeInstructionMode(0x70, (sbyte)data[1], 2)),
            // CLC
            0x18 => new Instruction(InstructionName.CLC, new ImpliedInstructionMode(0x18, 2)),
            // CLD
            0xD8 => new Instruction(InstructionName.CLD, new ImpliedInstructionMode(0xD8, 2)),
            // CLI
            0x58 => new Instruction(InstructionName.CLI, new ImpliedInstructionMode(0x58, 2)),
            // CLV
            0xB8 => new Instruction(InstructionName.CLV, new ImpliedInstructionMode(0xB8, 2)),
            // CMP
            0xC9 => new Instruction(InstructionName.CMP, new ImmediateInstructionMode(0xC9, data[1], 2)),
            0xC5 => new Instruction(InstructionName.CMP, new ZeroPageInstructionMode(0xC5, data[1], 3)),
            0xD5 => new Instruction(InstructionName.CMP, new ZeroPageXInstructionMode(0xD5, data[1], 4)),
            0xCD => new Instruction(InstructionName.CMP, new AbsoluteInstructionMode(0xCD, data[1], data[2], 4)),
            0xDD => new Instruction(InstructionName.CMP, new AbsoluteXInstructionMode(0xDD, data[1], data[2], 4)),
            0xD9 => new Instruction(InstructionName.CMP, new AbsoluteYInstructionMode(0xD9, data[1], data[2], 4)),
            0xC1 => new Instruction(InstructionName.CMP, new IndirectXInstructionMode(0xC1, data[1], 6)),
            0xD1 => new Instruction(InstructionName.CMP, new IndirectYInstructionMode(0xD1, data[1], 5)),
            // CPX
            0xE0 => new Instruction(InstructionName.CPX, new ImmediateInstructionMode(0xE0, data[1], 2)),
            0xE4 => new Instruction(InstructionName.CPX, new ZeroPageInstructionMode(0xE4, data[1], 3)),
            0xEC => new Instruction(InstructionName.CPX, new AbsoluteInstructionMode(0xEC, data[1], data[2], 4)),
            // CPY
            0xC0 => new Instruction(InstructionName.CPY, new ImmediateInstructionMode(0xC0, data[1], 2)),
            0xC4 => new Instruction(InstructionName.CPY, new ZeroPageInstructionMode(0xC4, data[1], 3)),
            0xCC => new Instruction(InstructionName.CPY, new AbsoluteInstructionMode(0xCC, data[1], data[2], 4)),
            // DEC
            0xC6 => new Instruction(InstructionName.DEC, new ZeroPageInstructionMode(0xC6, data[1], 5)),
            0xD6 => new Instruction(InstructionName.DEC, new ZeroPageXInstructionMode(0xD6, data[1], 6)),
            0xCE => new Instruction(InstructionName.DEC, new AbsoluteInstructionMode(0xCE, data[1], data[2], 6)),
            0xDE => new Instruction(InstructionName.DEC, new AbsoluteXInstructionMode(0xDE, data[1], data[2], 7)),
            // DEX
            0xCA => new Instruction(InstructionName.DEX, new ImpliedInstructionMode(0xCA, 2)),
            // DEY
            0x88 => new Instruction(InstructionName.DEY, new ImpliedInstructionMode(0x88, 2)),
            // EOR
            0x49 => new Instruction(InstructionName.EOR, new ImmediateInstructionMode(0x49, data[1], 2)),
            0x45 => new Instruction(InstructionName.EOR, new ZeroPageInstructionMode(0x45, data[1], 3)),
            0x55 => new Instruction(InstructionName.EOR, new ZeroPageXInstructionMode(0x55, data[1], 4)),
            0x4D => new Instruction(InstructionName.EOR, new AbsoluteInstructionMode(0x4D, data[1], data[2], 4)),
            0x5D => new Instruction(InstructionName.EOR, new AbsoluteXInstructionMode(0x5D, data[1], data[2], 4)),
            0x59 => new Instruction(InstructionName.EOR, new AbsoluteYInstructionMode(0x59, data[1], data[2], 4)),
            0x41 => new Instruction(InstructionName.EOR, new IndirectXInstructionMode(0x41, data[1], 6)),
            0x51 => new Instruction(InstructionName.EOR, new IndirectYInstructionMode(0x51, data[1], 5)),
            // INC
            0xE6 => new Instruction(InstructionName.INC, new ZeroPageInstructionMode(0xE6, data[1], 5)),
            0xF6 => new Instruction(InstructionName.INC, new ZeroPageXInstructionMode(0xF6, data[1], 6)),
            0xEE => new Instruction(InstructionName.INC, new AbsoluteInstructionMode(0xEE, data[1], data[2], 6)),
            0xFE => new Instruction(InstructionName.INC, new AbsoluteXInstructionMode(0xFE, data[1], data[2], 7)),
            // INX
            0xE8 => new Instruction(InstructionName.INX, new ImpliedInstructionMode(0xE8, 2)),
            // INY
            0xC8 => new Instruction(InstructionName.INY, new ImpliedInstructionMode(0xC8, 2)),
            // JMP
            0x4C => new Instruction(InstructionName.JMP, new AbsoluteInstructionMode(0x4C, data[1], data[2], 3)),
            0x6C => new Instruction(InstructionName.JMP, new IndirectInstructionMode(0x6C, data[1], data[2], 5)),
            // JSR
            0x20 => new Instruction(InstructionName.JSR, new AbsoluteInstructionMode(0x20, data[1], data[2], 6)),
            // LDA
            0xA9 => new Instruction(InstructionName.LDA, new ImmediateInstructionMode(0xA9, data[1], 2)),
            0xA5 => new Instruction(InstructionName.LDA, new ZeroPageInstructionMode(0xA5, data[1], 3)),
            0xB5 => new Instruction(InstructionName.LDA, new ZeroPageXInstructionMode(0xB5, data[1], 4)),
            0xAD => new Instruction(InstructionName.LDA, new AbsoluteInstructionMode(0xAD, data[1], data[2], 4)),
            0xBD => new Instruction(InstructionName.LDA, new AbsoluteXInstructionMode(0xBD, data[1], data[2], 4)),
            0xB9 => new Instruction(InstructionName.LDA, new AbsoluteYInstructionMode(0xB9, data[1], data[2], 4)),
            0xA1 => new Instruction(InstructionName.LDA, new IndirectXInstructionMode(0xA1, data[1], 6)),
            0xB1 => new Instruction(InstructionName.LDA, new IndirectYInstructionMode(0xB1, data[1], 5)),
            // LDX
            0xA2 => new Instruction(InstructionName.LDX, new ImmediateInstructionMode(0xA2, data[1], 2)),
            0xA6 => new Instruction(InstructionName.LDX, new ZeroPageInstructionMode(0xA6, data[1], 3)),
            0xB6 => new Instruction(InstructionName.LDX, new ZeroPageYInstructionMode(0xB6, data[1], 4)),
            0xAE => new Instruction(InstructionName.LDX, new AbsoluteInstructionMode(0xAE, data[1], data[2], 4)),
            0xBE => new Instruction(InstructionName.LDX, new AbsoluteYInstructionMode(0xBE, data[1], data[2], 4)),
            // LDY
            0xA0 => new Instruction(InstructionName.LDY, new ImmediateInstructionMode(0xA0, data[1], 2)),
            0xA4 => new Instruction(InstructionName.LDY, new ZeroPageInstructionMode(0xA4, data[1], 3)),
            0xB4 => new Instruction(InstructionName.LDY, new ZeroPageXInstructionMode(0xB4, data[1], 4)),
            0xAC => new Instruction(InstructionName.LDY, new AbsoluteInstructionMode(0xAC, data[1], data[2], 4)),
            0xBC => new Instruction(InstructionName.LDY, new AbsoluteXInstructionMode(0xBC, data[1], data[2], 4)),
            // LSR
            0x4A => new Instruction(InstructionName.LSR, new AccumulatorInstructionMode(0x4A, 2)),
            0x46 => new Instruction(InstructionName.LSR, new ZeroPageInstructionMode(0x46, data[1], 5)),
            0x56 => new Instruction(InstructionName.LSR, new ZeroPageXInstructionMode(0x56, data[1], 6)),
            0x4E => new Instruction(InstructionName.LSR, new AbsoluteInstructionMode(0x4E, data[1], data[2], 6)),
            0x5E => new Instruction(InstructionName.LSR, new AbsoluteXInstructionMode(0x5E, data[1], data[2], 7)),
            // NOP
            0xEA => new Instruction(InstructionName.NOP, new ImpliedInstructionMode(0xEA, 2)),
            // ORA
            0x09 => new Instruction(InstructionName.ORA, new ImmediateInstructionMode(0x09, data[1], 2)),
            0x05 => new Instruction(InstructionName.ORA, new ZeroPageInstructionMode(0x05, data[1], 3)),
            0x15 => new Instruction(InstructionName.ORA, new ZeroPageXInstructionMode(0x15, data[1], 4)),
            0x0D => new Instruction(InstructionName.ORA, new AbsoluteInstructionMode(0x0D, data[1], data[2], 4)),
            0x1D => new Instruction(InstructionName.ORA, new AbsoluteXInstructionMode(0x1D, data[1], data[2], 4)),
            0x19 => new Instruction(InstructionName.ORA, new AbsoluteYInstructionMode(0x19, data[1], data[2], 4)),
            0x01 => new Instruction(InstructionName.ORA, new IndirectXInstructionMode(0x01, data[1], 6)),
            0x11 => new Instruction(InstructionName.ORA, new IndirectYInstructionMode(0x11, data[1], 5)),
            // PHA
            0x48 => new Instruction(InstructionName.PHA, new ImpliedInstructionMode(0x48, 3)),
            // PHP
            0x08 => new Instruction(InstructionName.PHP, new ImpliedInstructionMode(0x08, 3)),
            // PLA
            0x68 => new Instruction(InstructionName.PLA, new ImpliedInstructionMode(0x68, 4)),
            // PLP
            0x28 => new Instruction(InstructionName.PLP, new ImpliedInstructionMode(0x28, 4)),
            // ROL
            0x2A => new Instruction(InstructionName.ROL, new AccumulatorInstructionMode(0x2A, 2)),
            0x26 => new Instruction(InstructionName.ROL, new ZeroPageInstructionMode(0x26, data[1], 5)),
            0x36 => new Instruction(InstructionName.ROL, new ZeroPageXInstructionMode(0x36, data[1], 6)),
            0x2E => new Instruction(InstructionName.ROL, new AbsoluteInstructionMode(0x2E, data[1], data[2], 6)),
            0x3E => new Instruction(InstructionName.ROL, new AbsoluteXInstructionMode(0x3E, data[1], data[2], 7)),
            // ROR
            0x6A => new Instruction(InstructionName.ROR, new AccumulatorInstructionMode(0x6A, 2)),
            0x66 => new Instruction(InstructionName.ROR, new ZeroPageInstructionMode(0x66, data[1], 5)),
            0x76 => new Instruction(InstructionName.ROR, new ZeroPageXInstructionMode(0x76, data[1], 6)),
            0x6E => new Instruction(InstructionName.ROR, new AbsoluteInstructionMode(0x6E, data[1], data[2], 6)),
            0x7E => new Instruction(InstructionName.ROR, new AbsoluteXInstructionMode(0x7E, data[1], data[2], 7)),
            // RTI
            0x40 => new Instruction(InstructionName.RTI, new ImpliedInstructionMode(0x40, 6)),
            // RTS
            0x60 => new Instruction(InstructionName.RTS, new ImpliedInstructionMode(0x60, 6)),
            // SBC
            0xE9 => new Instruction(InstructionName.SBC, new ImmediateInstructionMode(0xE9, data[1], 2)),
            0xE5 => new Instruction(InstructionName.SBC, new ZeroPageInstructionMode(0xE5, data[1], 3)),
            0xF5 => new Instruction(InstructionName.SBC, new ZeroPageXInstructionMode(0xF5, data[1], 4)),
            0xED => new Instruction(InstructionName.SBC, new AbsoluteInstructionMode(0xED, data[1], data[2], 4)),
            0xFD => new Instruction(InstructionName.SBC, new AbsoluteXInstructionMode(0xFD, data[1], data[2], 4)),
            0xF9 => new Instruction(InstructionName.SBC, new AbsoluteYInstructionMode(0xF9, data[1], data[2], 4)),
            0xE1 => new Instruction(InstructionName.SBC, new IndirectXInstructionMode(0xE1, data[1], 6)),
            0xF1 => new Instruction(InstructionName.SBC, new IndirectYInstructionMode(0xF1, data[1], 5)),
            // SEC
            0x38 => new Instruction(InstructionName.SEC, new ImpliedInstructionMode(0x38, 2)),
            // SED
            0xF8 => new Instruction(InstructionName.SED, new ImpliedInstructionMode(0xF8, 2)),
            // SEI
            0x78 => new Instruction(InstructionName.SEI, new ImpliedInstructionMode(0x78, 2)),
            // STA
            0x85 => new Instruction(InstructionName.STA, new ZeroPageInstructionMode(0x85, data[1], 3)),
            0x95 => new Instruction(InstructionName.STA, new ZeroPageXInstructionMode(0x95, data[1], 4)),
            0x8D => new Instruction(InstructionName.STA, new AbsoluteInstructionMode(0x8D, data[1], data[2], 4)),
            0x9D => new Instruction(InstructionName.STA, new AbsoluteXInstructionMode(0x9D, data[1], data[2], 5)),
            0x99 => new Instruction(InstructionName.STA, new AbsoluteYInstructionMode(0x99, data[1], data[2], 5)),
            0x81 => new Instruction(InstructionName.STA, new IndirectXInstructionMode(0x81, data[1], 6)),
            0x91 => new Instruction(InstructionName.STA, new IndirectYInstructionMode(0x91, data[1], 6)),
            // STX
            0x86 => new Instruction(InstructionName.STX, new ZeroPageInstructionMode(0x86, data[1], 3)),
            0x96 => new Instruction(InstructionName.STX, new ZeroPageYInstructionMode(0x96, data[1], 4)),
            0x8E => new Instruction(InstructionName.STX, new AbsoluteInstructionMode(0x8E, data[1], data[2], 4)),
            // STY
            0x84 => new Instruction(InstructionName.STY, new ZeroPageInstructionMode(0x84, data[1], 3)),
            0x94 => new Instruction(InstructionName.STY, new ZeroPageXInstructionMode(0x94, data[1], 4)),
            0x8C => new Instruction(InstructionName.STY, new AbsoluteInstructionMode(0x8C, data[1], data[2], 4)),
            // TAX
            0xAA => new Instruction(InstructionName.TAX, new ImpliedInstructionMode(0xAA, 2)),
            // TAY
            0xA8 => new Instruction(InstructionName.TAY, new ImpliedInstructionMode(0xA8, 2)),
            // TSX
            0xBA => new Instruction(InstructionName.TSX, new ImpliedInstructionMode(0xBA, 2)),
            // TXA
            0x8A => new Instruction(InstructionName.TXA, new ImpliedInstructionMode(0x8A, 2)),
            // TXS
            0x9A => new Instruction(InstructionName.TXS, new ImpliedInstructionMode(0x9A, 2)),
            // TYA
            0x98 => new Instruction(InstructionName.TYA, new ImpliedInstructionMode(0x98, 2)),
            _ => null,
        };
    }
}
