using System.Collections;
using Modern.Vice.PdbMonitor.Engine.Models.OpCodes;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using NUnit.Framework;
using TestsBase;

namespace Modern.Vice.PdbMonitor.Engine.Test.Services.Implementation;
internal class DisassemblerTest : BaseTest<Disassembler>
{
    public  class DisassembleInstructionTest
    {
        static TestCaseData CreateData(byte[] data) => new TestCaseData(data);
        public static IEnumerable TestADC
        {
            get
            {
                InstructionName name = InstructionName.ADC;
                yield return CreateData([0x69, 0x10]).Returns(new Instruction(name, new ImmediateInstructionMode(0x69, 0x10, 2)));
                yield return CreateData([0x65, 0x10]).Returns(new Instruction(name, new ZeroPageInstructionMode(0x65, 0x10, 3)));
                yield return CreateData([0x75, 0x10]).Returns(new Instruction(name, new ZeroPageXInstructionMode(0x75, 0x10, 4)));
                yield return CreateData([0x6D, 0x10, 0x99]).Returns(new Instruction(name, new AbsoluteInstructionMode(0x6D, 0x10, 0x99, 4)));
                yield return CreateData([0x7D, 0x10, 0x99]).Returns(new Instruction(name, new AbsoluteXInstructionMode(0x7D, 0x10, 0x99, 4)));
                yield return CreateData([0x79, 0x10, 0x99]).Returns(new Instruction(name, new AbsoluteYInstructionMode(0x79, 0x10, 0x99, 4)));
                yield return CreateData([0x61, 0x10]).Returns(new Instruction(name, new IndirectXInstructionMode(0x61, 0x10, 6)));
                yield return CreateData([0x71, 0x10]).Returns(new Instruction(name, new IndirectYInstructionMode(0x71, 0x10, 5)));
            }
        }
        public static IEnumerable TestAND
        {
            get
            {
                InstructionName name = InstructionName.AND;
                yield return CreateData([0x29, 0x10]).Returns(new Instruction(name, new ImmediateInstructionMode(0x29, 0x10, 2)));
                yield return CreateData([0x25, 0x10]).Returns(new Instruction(name, new ZeroPageInstructionMode(0x25, 0x10, 3)));
                yield return CreateData([0x35, 0x10]).Returns(new Instruction(name, new ZeroPageXInstructionMode(0x35, 0x10, 4)));
                yield return CreateData([0x2D, 0x10, 0x99]).Returns(new Instruction(name, new AbsoluteInstructionMode(0x2D, 0x10, 0x99, 4)));
                yield return CreateData([0x3D, 0x10, 0x99]).Returns(new Instruction(name, new AbsoluteXInstructionMode(0x3D, 0x10, 0x99, 4)));
                yield return CreateData([0x39, 0x10, 0x99]).Returns(new Instruction(name, new AbsoluteYInstructionMode(0x39, 0x10, 0x99, 4)));
                yield return CreateData([0x21, 0x10]).Returns(new Instruction(name, new IndirectXInstructionMode(0x21, 0x10, 6)));
                yield return CreateData([0x31, 0x10]).Returns(new Instruction(name, new IndirectYInstructionMode(0x31, 0x10, 5)));
            }
        }
        public static IEnumerable TestASL
        {
            get
            {
                InstructionName name = InstructionName.ASL;
                yield return CreateData([0x0A, 0x10]).Returns(new Instruction(name, new AccumulatorInstructionMode(0x0A, 2)));
                yield return CreateData([0x06, 0x10]).Returns(new Instruction(name, new ZeroPageInstructionMode(0x06, 0x10, 5)));
                yield return CreateData([0x16, 0x10]).Returns(new Instruction(name, new ZeroPageXInstructionMode(0x16, 0x10, 6)));
                yield return CreateData([0x0E, 0x10, 0x99]).Returns(new Instruction(name, new AbsoluteInstructionMode(0x0E, 0x10, 0x99, 6)));
                yield return CreateData([0x1E, 0x10, 0x99]).Returns(new Instruction(name, new AbsoluteXInstructionMode(0x1E, 0x10, 0x99, 7)));
            }
        }
        public static IEnumerable TestBCC
        {
            get
            {
                InstructionName name = InstructionName.BCC;
                yield return CreateData([0x90, 0x10]).Returns(new Instruction(name, new RelativeInstructionMode(0x90, 0x10, 2)));
            }
        }
        public static IEnumerable TestBCS
        {
            get
            {
                InstructionName name = InstructionName.BCS;
                yield return CreateData([0xB0, 0x10]).Returns(new Instruction(name, new RelativeInstructionMode(0xB0, 0x10, 2)));
            }
        }
        public static IEnumerable TestBEQ
        {
            get
            {
                InstructionName name = InstructionName.BEQ;
                yield return CreateData([0xF0, 0x10]).Returns(new Instruction(name, new RelativeInstructionMode(0xF0, 0x10, 2)));
            }
        }
        public static IEnumerable TestBIT
        {
            get
            {
                InstructionName name = InstructionName.BIT;
                yield return CreateData([0x24, 0x10]).Returns(new Instruction(name, new ZeroPageInstructionMode(0x24, 0x10, 3)));
                yield return CreateData([0x2C, 0x10, 0x99]).Returns(new Instruction(name, new AbsoluteInstructionMode(0x2C, 0x10, 0x99, 4)));
            }
        }
        public static IEnumerable TestBMI
        {
            get
            {
                InstructionName name = InstructionName.BMI;
                yield return CreateData([0x30, 0x10]).Returns(new Instruction(name, new RelativeInstructionMode(0x30, 0x10, 2)));
            }
        }
        public static IEnumerable TestBNE
        {
            get
            {
                InstructionName name = InstructionName.BNE;
                yield return CreateData([0xD0, 0x10]).Returns(new Instruction(name, new RelativeInstructionMode(0xD0, 0x10, 2)));
            }
        }
        public static IEnumerable TestBPL
        {
            get
            {
                InstructionName name = InstructionName.BPL;
                yield return CreateData([0x10, 0x10]).Returns(new Instruction(name, new RelativeInstructionMode(0x10, 0x10, 2)));
            }
        }
        public static IEnumerable TestBRK
        {
            get
            {
                InstructionName name = InstructionName.BRK;
                yield return CreateData([0x00]).Returns(new Instruction(name, new ImpliedInstructionMode(0x00, 7)));
            }
        }
    }
    [TestFixture]
    public class DisassembleInstruction : DisassemblerTest
    {
        [TestCaseSource(typeof(DisassembleInstructionTest), nameof(DisassembleInstructionTest.TestADC))]
        public Instruction TestADC(byte[] data)
        {
            return Target.DisassembleInstruction(data);
        }
        [TestCaseSource(typeof(DisassembleInstructionTest), nameof(DisassembleInstructionTest.TestAND))]
        public Instruction TestAND(byte[] data)
        {
            return Target.DisassembleInstruction(data);
        }
        [TestCaseSource(typeof(DisassembleInstructionTest), nameof(DisassembleInstructionTest.TestASL))]
        public Instruction TestASL(byte[] data)
        {
            return Target.DisassembleInstruction(data);
        }
        [TestCaseSource(typeof(DisassembleInstructionTest), nameof(DisassembleInstructionTest.TestBCC))]
        public Instruction TestBCC(byte[] data)
        {
            return Target.DisassembleInstruction(data);
        }
        [TestCaseSource(typeof(DisassembleInstructionTest), nameof(DisassembleInstructionTest.TestBCS))]
        public Instruction TestBCS(byte[] data)
        {
            return Target.DisassembleInstruction(data);
        }
        [TestCaseSource(typeof(DisassembleInstructionTest), nameof(DisassembleInstructionTest.TestBEQ))]
        public Instruction TestBEQ(byte[] data)
        {
            return Target.DisassembleInstruction(data);
        }
        [TestCaseSource(typeof(DisassembleInstructionTest), nameof(DisassembleInstructionTest.TestBIT))]
        public Instruction TestBIT(byte[] data)
        {
            return Target.DisassembleInstruction(data);
        }
        [TestCaseSource(typeof(DisassembleInstructionTest), nameof(DisassembleInstructionTest.TestBMI))]
        public Instruction TestBMI(byte[] data)
        {
            return Target.DisassembleInstruction(data);
        }
        [TestCaseSource(typeof(DisassembleInstructionTest), nameof(DisassembleInstructionTest.TestBNE))]
        public Instruction TestBNE(byte[] data)
        {
            return Target.DisassembleInstruction(data);
        }
        [TestCaseSource(typeof(DisassembleInstructionTest), nameof(DisassembleInstructionTest.TestBPL))]
        public Instruction TestBPL(byte[] data)
        {
            return Target.DisassembleInstruction(data);
        }
        [TestCaseSource(typeof(DisassembleInstructionTest), nameof(DisassembleInstructionTest.TestBRK))]
        public Instruction TestBRK(byte[] data)
        {
            return Target.DisassembleInstruction(data);
        }
    }
}
