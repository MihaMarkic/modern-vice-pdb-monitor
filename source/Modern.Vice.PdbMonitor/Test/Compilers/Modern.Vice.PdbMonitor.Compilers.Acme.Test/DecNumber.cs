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
            Assert.DoesNotThrow(() => Run(input, p => p.decnumber()));
        }

        [TestCase("a55")]
        [TestCase("100b")]
        [TestCase("a100b")]
        [TestCase("aa")]
        [TestCase("")]
        public void TestInvalid(string input)
        {
            Assert.Throws<Exception>(() => Run(input, p => p.decnumber()));
        }
    }
}
