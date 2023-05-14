using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Compilers.Acme.Services.Implementation;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Core.Common.Compiler;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using NSubstitute;
using NUnit.Framework;

namespace Modern.Vice.PdbMonitor.Engine.Test.Services.Implementation;

/// <summary>
/// 
/// </summary>
/// <remarks>Can't use BaseTest/NFixture because of circular references</remarks>
class PdbManagerTest
{
    protected PdbManager Target;

    [SetUp]
    public void SetUp()
    {
        var globals = new Globals(Substitute.For<ILogger<Globals>>(), Substitute.For<ISettingsManager>());
        Target = new PdbManager(Substitute.For<ILogger<PdbManager>>(), globals);
    }

    // TODO eventually create compiler independent source files and get rid of ACME reference
    [TestFixture]
    public class BinarySearch : PdbManagerTest
    {
        Pdb pdb;
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
            var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x1300);

            Assert.That(actual, Is.EqualTo(pdb.LinesWithAddress[0]));
        }
        [Test]
        public void WhenSearchingBeforeFirstAddress_ReturnsNull()
        {
            var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x1200);

            Assert.That(actual, Is.Null);
        }
        [Test]
        public void WhenSearchingLastAddress_ReturnsLastLine()
        {
            var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x3014);

            Assert.That(actual, Is.EqualTo(pdb.LinesWithAddress.Last()));
        }
        [Test]
        public void WhenSearchingAfterLastAddress_ReturnsNull()
        {
            var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x4000);

            Assert.That(actual, Is.Null);
        }
        [Test]
        public void WhenSearchingInTheUpperAddresses_ReturnsCorrectLine()
        {
            var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x2663);

            Assert.That(actual.IsAddressWithinLine(0x2663), Is.True);
        }
        [Test]
        public void WhenSearchingInTheUpperNonStartAddress_ReturnsCorrectLine()
        {
            var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x2664);

            Assert.That(actual.IsAddressWithinLine(0x2663), Is.True);
        }
        [Test]
        public void WhenSearchingInTheLowerAddresses_ReturnsCorrectLine()
        {
            var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x1cbb);

            Assert.That(actual.IsAddressWithinLine(0x1cbb), Is.True);
        }
        [Test]
        public void WhenSearchingInTheLowerNonStartAddress_ReturnsCorrectLine()
        {
            var actual = Target.BinarySearch(pdb.LinesWithAddress, 0x1cbc);

            Assert.That(actual.IsAddressWithinLine(0x1cbb), Is.True);
        }
    }

    public class Tubo
    {
        public Tubo(ImmutableArray<int>? Data) { }
    }
}
