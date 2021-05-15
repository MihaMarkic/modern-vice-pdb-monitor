using System.Linq;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Modern.Vice.PdbMonitor.Utils.Test;
using NUnit.Framework;

namespace Modern.Vice.PdbMonitor.Engine.Test.Services.Implementation
{
    class AcmePdbParserTest:BaseTest<AcmePdbParser>
    {
        [TestFixture]
        public class ParseLabel: AcmePdbParserTest
        {
            [TestCase("al C:001a .bckgrnd", ExpectedResult = (ushort)0x001a)]
            [TestCase("al C:0007 .hWindow_NoMemLeft", ExpectedResult = (ushort)0x0007)]
            public ushort GivenSampleLine_ParsesAddressCorrectly(string line)
            {
                var actual = Target.ParseLabel(line, 1, new AcmePdbParser.Context());

                return actual.Address;
            }
            [TestCase("al C:001a .bckgrnd", ExpectedResult = "bckgrnd")]
            [TestCase("al C:0007 .hWindow_NoMemLeft", ExpectedResult = "hWindow_NoMemLeft")]
            public string GivenSampleLine_ParsesNameCorrectly(string line)
            {
                return Target.ParseLabel(line, 1, new AcmePdbParser.Context()).Name;
            }
            [TestCase("")]
            [TestCase(" ")]
            public void GivenEmptyLine_ReturnsNull(string line)
            {
                var actual = Target.ParseLabel(line, 1, new AcmePdbParser.Context());

                Assert.That(actual, Is.Null);
            }
        }
        [TestFixture]
        public class ParseCodeLine : AcmePdbParserTest
        {
            [TestCase("    17                          ; Code:", ExpectedResult = 17)]
            [TestCase("  1119  2c88 ae0123             		ldx menunr	; Holt Nummer", ExpectedResult = 1119)]
            [TestCase("    24  130d 3014d31376131913...	!binary \"me/tables.bin\", 826", ExpectedResult = 24)]
            public int GivenSampleLine_ParsesLineNumberCorrectly(string line)
            {
                var actual = (ReportCodeLine)Target.ParseCodeLine(line, 1, new AcmePdbParser.Context());
                return actual.LineNumber;
            }
            [TestCase("    17                          ; Code:", ExpectedResult = null)]
            [TestCase("  1119  2c88 ae0123             		ldx menunr	; Holt Nummer", ExpectedResult = (ushort)0x2c88)]
            [TestCase("    24  130d 3014d31376131913...	!binary \"me/tables.bin\", 826", ExpectedResult = (ushort)0x130d)]
            public ushort? GivenSampleLine_ParsesAddressCorrectly(string line)
            {
                var actual = (ReportCodeLine)Target.ParseCodeLine(line, 1, new AcmePdbParser.Context());
                return actual.StartAddress;
            }
            [TestCase("    17                          ; Code:", ExpectedResult = "")]
            [TestCase("  1119  2c88 ae0123             		ldx menunr	; Holt Nummer", ExpectedResult = "ae0123")]
            [TestCase("    24  130d 3014d31376131913...	!binary \"me/tables.bin\", 826", ExpectedResult = "3014d31376131913")]
            public string? GivenSampleLine_ParsesBytesCorrectly(string line)
            {
                var actual = (ReportCodeLine)Target.ParseCodeLine(line, 1, new AcmePdbParser.Context());

                string result = string.Join("", actual.Data.Select(b => b.ToString("x2")));
                return result;
            }
            [TestCase("    17                          ; Code:", ExpectedResult = null)]
            [TestCase("  1119  2c88 ae0123             		ldx menunr	; Holt Nummer", ExpectedResult = false)]
            [TestCase("    24  130d 3014d31376131913...	!binary \"me/tables.bin\", 826", ExpectedResult = true)]
            public bool? GivenSampleLine_ParsesIsMoreDataCorrectly(string line)
            {
                var actual = (ReportCodeLine)Target.ParseCodeLine(line, 1, new AcmePdbParser.Context());

                return actual.IsMoreData;
            }
            [TestCase("    17                          ; Code:", ExpectedResult = "; Code:")]
            [TestCase("  1119  2c88 ae0123             		ldx menunr	; Holt Nummer", ExpectedResult = "		ldx menunr	; Holt Nummer")]
            [TestCase("    24  130d 3014d31376131913...	!binary \"me/tables.bin\", 826", ExpectedResult = "	!binary \"me/tables.bin\", 826")]
            public string GivenSampleLine_ParsesLineCorrectly(string line)
            {
                var actual = (ReportCodeLine)Target.ParseCodeLine(line, 1, new AcmePdbParser.Context());

                return actual.Text;
            }
        }
    }
}
