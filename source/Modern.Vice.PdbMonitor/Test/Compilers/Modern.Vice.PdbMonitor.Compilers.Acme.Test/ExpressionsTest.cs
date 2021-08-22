using System;
using NUnit.Framework;

namespace Modern.Vice.PdbMonitor.Compilers.Acme.Test
{
    [TestFixture]
    public class DecNumber: Bootstrap
    {
        [TestCase("55")]
        [TestCase("100")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.decNumber()));
        }

        [TestCase("a55")]
        [TestCase("100b")]
        [TestCase("a100b")]
        [TestCase("aa")]
        [TestCase("")]
        public void TestInvalid(string input)
        {
            Assert.Throws<Exception>(() => Run(input, p => p.decNumber()));
        }
    }
    public class Expression: Bootstrap
    {
        [TestCase("55")]
        [TestCase("100")]
        [TestCase("100 + 55")]
        [TestCase("(100)")]
        [TestCase("(100+55)")]
        [TestCase("(100 + 55) * $8")]
        [TestCase("<h")]
        [TestCase("<*")]
        [TestCase(">h")]
        [TestCase(">*")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.expression()));
        }
        [TestCase("(100 + 55")]
        public void TestInvalid(string input)
        {
            Assert.Throws<Exception>(() => Run(input, p => p.expression()));
        }
    }

    public class ExpressionPseudoOps: Bootstrap
    {
        [TestCase("!8 $15")]
        [TestCase("!by $15")]
        [TestCase("!byte $15")]
        [TestCase("!16 $15")]
        [TestCase("!wo $15")]
        [TestCase("!word $15")]
        [TestCase("!le16 $15")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.expressionPseudoOps()));
        }
    }
    public class Binary: Bootstrap
    {
        [TestCase("!bin \"table\", 2, 9")]
        [TestCase("!bin \"list\",, 9")]
        [TestCase("!bin <list.tst>,, 9")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.binary()));
        }
    }

    public class If: Bootstrap
    {
        //[TestCase("if debug { !text \"Gray\" }")]
        [TestCase(
@"if debug { 
    !text ""Gray"" 
}")]
        [TestCase(
@"if debug {
    !text ""Gray"" 
}")]
        [TestCase(
@"if debug {

    !text ""Gray"" 
}")]
        [TestCase(
        @"if country == uk {
        			!text ""Grey""
                } else if country == fr {
        			!text ""Gris""
        		} else if country == de {
            !text ""Grau""
                }
        else
        {
            !text ""Gray""
                }
        ")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.ifFlow()));
        }
    }

    public class IfDef: Bootstrap
    {
        [TestCase("ifdef my_label {my_label}")]
        [TestCase("ifndef my_label {my_label}")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.ifDefFlow()));
        }
    }

    public class ForDef : Bootstrap
    {
        //[TestCase(@"for Inner, 0, 9 {
		//		!byte (Outer << 4) OR Inner
		//	}")]
        [TestCase(
@"for h in my_handler_list {
			!by >h
		}")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.forFlow()));
        }
    }

    public class Set: Bootstrap
    {
        [TestCase("!set a = a + 1")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.set()));
        }
    }

    public class Block: Bootstrap
    {
        [TestCase("{ !set a = a + 1 }")]
        [TestCase("{\n !set a = a + 1\n }")]
        [TestCase("{\n}")]
        [TestCase("{\n ; test comment\n}")]
        [TestCase("{\n .symbol ; test comment\n}")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.block()));
        }
    }

    public class DoFlow: Bootstrap
    {
        [TestCase("!do while * < $c000 { nop }")]
        // TODO enable code below
        [TestCase(
@"!do while loop_flag == TRUE {
			; lda #a
			; sta label + a
			!set a = a + 1
		} until a > 6")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.doFlow()));
        }
    }
    public class WhileFlow : Bootstrap
    {
        [TestCase("!while * < $c000 { nop }")]
        // TODO enable code below
        [TestCase(
@"!while a < 6 {
			; lda #a
			; sta label + a
			!set a = a + 1
		}")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.whileFlow()));
        }
    }

    public class Warn: Bootstrap
    {
        [TestCase("!warn \"Program reached ROM: \", * - $a000, \" bytes overlap.\"")]
        [TestCase("!error \"Program reached ROM: \", * - $a000, \" bytes overlap.\"")]
        [TestCase("!serious \"Program reached ROM: \", * - $a000, \" bytes overlap.\"")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.reportError()));
        }
    }

    public class Macro: Bootstrap
    {
        [TestCase(
@"!macro bne .target {
			; beq * + 5
			; jmp .target
		}")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.macro()));
        }
    }

    public class CallMacro: Bootstrap
    {
        [TestCase("+reserve ~.line_buffer, 80")]
        [TestCase("+dinc $fb")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.callMarco()));
        }
    }
    public class SetProgramCounter : Bootstrap
    {
        [TestCase("* = $0801")]
        [TestCase("* = $8010, overlay, invisible")]
        [TestCase("* = $15219, invisible, overlay")]
        [TestCase("* = 428, invisible")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.setProgramCounter()));
        }
    }
    public class Xor: Bootstrap
    {
        [TestCase(
@"!xor $80 {
		!scr ""Hello everybody..."", GROUPLOGOCHAR
        }")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.xor()));
        }
    }

    public class PseudoPc: Bootstrap
    {
        [TestCase(
@"!pseudopc $0400 {
	.target	; imagine some code here...
		; it should be copied to $0400 and executed *there*
		}")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.pseudoPc()));
        }
    }

    public class Statements: Bootstrap
    {
        //[TestCase(".symbol\n")]
        //[TestCase(".symbol ; comment\n")]
        [TestCase("; comment\n")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.statements()));
        }
    }
}
