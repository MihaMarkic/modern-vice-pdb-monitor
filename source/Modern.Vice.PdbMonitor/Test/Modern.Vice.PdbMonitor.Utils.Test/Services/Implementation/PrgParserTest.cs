using System.IO;
using AutoFixture;
using Modern.Vice.PdbMonitor.Core.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using NSubstitute;
using NUnit.Framework;
using TestsBase;

namespace Modern.Vice.PdbMonitor.Engine.Test.Services.Implementation;
internal class PrgParserTest: BaseTest<PrgParser>
{
    [TestFixture]
    public class GetStartAddress: PrgParserTest
    {
        [Test]
        public void GivenSampleData_ExtractsStartAddress()
        {
            var fileService = fixture.Freeze<IFileService>();
            fileService.OpenFileStream("path").Returns(new MemoryStream(
                [0x01, 0x08, 0x0B, 0x08, 0x0A, 0x00, 0x9E, 0x32, 0x30, 0x36, 0x31, 0x00]));

            var actual = Target.GetStartAddress("path");

            Assert.That(actual, Is.EqualTo(0x080D));
        }
    }
    [TestFixture]
    public class GetEntryAddress : PrgParserTest
    {
        [Test]
        public void GivenSampleData_ExtractsEntryAddress()
        {
            var fileService = fixture.Freeze<IFileService>();
            fileService.OpenFileStream("path").Returns(new MemoryStream([0x01, 0x08]));

            var actual = Target.GetEntryAddress("path");

            Assert.That(actual, Is.EqualTo(0x0801));
        }
    }
}
