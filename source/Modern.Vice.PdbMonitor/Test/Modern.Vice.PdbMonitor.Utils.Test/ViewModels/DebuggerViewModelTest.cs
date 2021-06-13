using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using Modern.Vice.PdbMonitor.Utils.Test;
using NUnit.Framework;

namespace Modern.Vice.PdbMonitor.Engine.Test.ViewModels
{
    class DebuggerViewModelTest: BaseTest<DebuggerViewModel>
    {
        [TestFixture]
        public class BinarySearch: DebuggerViewModelTest
        {
            AcmePdb pdb;
            [SetUp]
            public async Task SetUpAsync()
            {
                var projectDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples");
                string repPath = Path.Combine(projectDirectory, "macedit.rep");
                string lblPath = Path.Combine(projectDirectory, "macedit.lbl");
                var debugFiles = new DebugFiles(repPath, lblPath);
                var parser = new AcmePdbParser();
                var result = await parser.ParseAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples"), debugFiles);
                pdb = result.ParsedData;
            }
            [Test]
            public void WhenSearchingFirstAddress_ReturnsFirstLine()
            {
                var x = fixture.Build<Tubo>().OmitAutoProperties().Create();
                var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x1300);

                Assert.That(actual, Is.EqualTo(pdb.LinesWithAddress[0]));
            }
            [Test]
            public void WhenSearchingBeforeFirstAddress_ReturnsNull()
            {
                var x = fixture.Build<Tubo>().OmitAutoProperties().Create();
                var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x1200);

                Assert.That(actual, Is.Null);
            }
            [Test]
            public void WhenSearchingLastAddress_ReturnsLastLine()
            {
                var x = fixture.Build<Tubo>().OmitAutoProperties().Create();
                var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x3014);

                Assert.That(actual, Is.EqualTo(pdb.LinesWithAddress.Last()));
            }
            [Test]
            public void WhenSearchingAfterLastAddress_ReturnsNull()
            {
                var x = fixture.Build<Tubo>().OmitAutoProperties().Create();
                var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x4000);

                Assert.That(actual, Is.Null);
            }
            [Test]
            public void WhenSearchingInTheUpperAddresses_ReturnsCorrectLine()
            {
                var x = fixture.Build<Tubo>().OmitAutoProperties().Create();
                var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x2663);

                Assert.That(actual.StartAddress, Is.EqualTo(0x2663));
            }
            [Test]
            public void WhenSearchingInTheUpperNonStartAddress_ReturnsCorrectLine()
            {
                var x = fixture.Build<Tubo>().OmitAutoProperties().Create();
                var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x2664);

                Assert.That(actual.StartAddress, Is.EqualTo(0x2663));
            }
            [Test]
            public void WhenSearchingInTheLowerAddresses_ReturnsCorrectLine()
            {
                var x = fixture.Build<Tubo>().OmitAutoProperties().Create();
                var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x1cbb);

                Assert.That(actual.StartAddress, Is.EqualTo(0x1cbb));
            }
            [Test]
            public void WhenSearchingInTheLowerNonStartAddress_ReturnsCorrectLine()
            {
                var x = fixture.Build<Tubo>().OmitAutoProperties().Create();
                var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x1cbc);

                Assert.That(actual.StartAddress, Is.EqualTo(0x1cbb));
            }
        }

        public class Tubo
        {
            public Tubo(ImmutableArray<int>? Data) { }
        }
    }
}
