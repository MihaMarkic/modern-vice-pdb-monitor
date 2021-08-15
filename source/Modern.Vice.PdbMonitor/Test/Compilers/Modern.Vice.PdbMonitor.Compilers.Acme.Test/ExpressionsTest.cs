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
        [TestCase("if debug { !text \"Gray\" }")]
        [TestCase(@"
if debug 
{ 
    !text ""Gray"" 
}")]
        [TestCase(@"
if debug {
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
}
