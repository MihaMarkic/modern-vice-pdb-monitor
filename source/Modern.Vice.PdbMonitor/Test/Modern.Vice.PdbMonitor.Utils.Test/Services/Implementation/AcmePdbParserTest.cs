using System.IO;
using System.Threading.Tasks;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Modern.Vice.PdbMonitor.Utils.Test;
using NUnit.Framework;

namespace Modern.Vice.PdbMonitor.Engine.Test.Services.Implementation
{
    class AcmePdbParserTest: BaseTest<AcmePdbParser>
    {
        [TestFixture]
        public class ParseAsync: AcmePdbParserTest
        {
            [Test]
            public async Task GivenSample_ParsesCorrectly()
            {
                string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples", "SamplePdbFile.pdb");

                var actual = await Target.ParseAsync(path);

                Assert.That(actual.Errors.Length, Is.Zero);
                Assert.That(actual.AcmePdb.Includes.Length, Is.EqualTo(1));
                Assert.That(actual.AcmePdb.Files.Length, Is.EqualTo(2));
                Assert.That(actual.AcmePdb.Addresses.Length, Is.EqualTo(981));
                Assert.That(actual.AcmePdb.Labels.Length, Is.EqualTo(18));
            }
        }

        [TestFixture]
        public class ParseCaption: AcmePdbParserTest
        {
            [Test]
            public void WhenValidCaption_ParsesCorrectly()
            {
                var context = new AcmePdbParser.Context();
                var actual = Target.ParseCaption(1, "ADDRS:981", context);

                Assert.That(actual, Is.EqualTo(("ADDRS", 981)));
            }
            [Test]
            public void WhenValidCaption_NoErrors()
            {
                var context = new AcmePdbParser.Context();
                var actual = Target.ParseCaption(1, "ADDRS:981", context);

                Assert.That(context.Errors.Count, Is.Zero);
            }
            [Test]
            public void WhenInValidCount_NullIsReturned()
            {
                var context = new AcmePdbParser.Context();
                var actual = Target.ParseCaption(1, "ADDRS:98x1", context);

                Assert.That(actual, Is.Null);
            }
        }

        [TestFixture]
        public class GetFile : AcmePdbParserTest
        {
            [Test]
            public void GivenSampleValidLine_ParsesCorrectly()
            {
                var actual = Target.GetFile(0, "1:ACME_Lib/6502/std.a", new AcmePdbParser.Context());

                Assert.That(actual, Is.EqualTo(new AcmePdbFile(1, "ACME_Lib/6502/std.a")));
            }
        }
        [TestFixture]
        public class GetAddress : AcmePdbParserTest
        {
            [Test]
            public void GivenSampleValidLine_ParsesCorrectly()
            {
                var actual = Target.GetAddress(0, "$402:1:0:24", new AcmePdbParser.Context());

                Assert.That(actual, Is.EqualTo(new AcmePdbAddress(0x402, 1, 0, 24)));
            }
        }
        [TestFixture]
        public class GetLabel : AcmePdbParserTest
        {
            [Test]
            public void GivenSampleValidLine_ParsesCorrectly()
            {
                var actual = Target.GetLabel(0, "$427:1:localLabel:1:1", new AcmePdbParser.Context());

                Assert.That(actual, Is.EqualTo(new AcmePdbLabel(0x427, 1, "localLabel", true, true)));
            }
        }
    }
}
