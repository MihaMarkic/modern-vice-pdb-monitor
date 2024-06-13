using Assembler.KickAssembler.Models;
using NUnit.Framework;
using TestsBase;

namespace Assembler.KickAssembler.Services.Implementation;

public class KickAssemblerDbgParserTest: BaseTest<KickAssemblerDbgParser>
{
    [TestFixture]
    public class ParseSource : KickAssemblerDbgParserTest
    {
        [Test]
        public void GivenSampleKickAssJarLine_ParserCorrectly()
        {
            var actual = Target.ParseSource("0,KickAss.jar:/include/autoinclude.asm");
            
            Assert.That(actual, 
                Is.EqualTo(new Source(0, SourceOrigin.KickAss, "/include/autoinclude.asm")));
        }
        [Test]
        public void GivenSampleUserLine_ParserCorrectly()
        {
            var actual = Target.ParseSource("1,/Users/miha/Projects/c64/vs64/First/src/main.asm");
            
            Assert.That(actual, 
                Is.EqualTo(new Source(1, SourceOrigin.User, "/Users/miha/Projects/c64/vs64/First/src/main.asm")));
        }
    }
}
