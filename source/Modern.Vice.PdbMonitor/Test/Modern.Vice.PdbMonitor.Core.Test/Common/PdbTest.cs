using Modern.Vice.PdbMonitor.Core.Common;
using NUnit.Framework;

namespace Modern.Vice.PdbMonitor.Core.Test.Common;
internal class PdbTest
{
    internal class PdbPathTest: PdbTest
    {
        [TestFixture]
        public class Create: PdbPathTest
        {
            [TestCase(@"D:/Temp", @"D:\Temp\test.c", ExpectedResult = true)]
            [TestCase(@"D:/Temp", @"C:\Program Files\test.c", ExpectedResult = false)]
            public bool GivenTestCases_DetectsIfAbsoluteOrRelative(string directory, string path)
            {
                return PdbPath.Create(directory, path).IsRelative;
            }
            [TestCase(@"D:/Temp", @"D:\Temp\test.c", ExpectedResult = "test.c")]
            [TestCase(@"D:/Temp", @"D:\Temp\out\test.c", ExpectedResult = @"out\test.c")]
            public string GivenTestCases_ExtractsRelativePathCorrectly(string directory, string path)
            {
                return PdbPath.Create(directory, path).Path;
            }
        }
    }
}
