using Modern.Vice.PdbMonitor.Core.Extensions;
using NUnit.Framework;

namespace Modern.Vice.PdbMonitor.Core.Test.Extensions;
internal class MathExtensionTest
{
    [TestFixture]
    public class CalculateNumberOfDigits : MathExtensionTest
    {
        [TestCase(0, ExpectedResult = 1)]
        [TestCase(9, ExpectedResult = 1)]
        [TestCase(10, ExpectedResult = 2)]
        [TestCase(19, ExpectedResult = 2)]
        [TestCase(99, ExpectedResult = 2)]
        [TestCase(100, ExpectedResult = 3)]
        [TestCase(999, ExpectedResult = 3)]
        [TestCase(1000, ExpectedResult = 4)]
        public int Test(int number) => number.CalculateNumberOfDigits();
    }
}
