using System.Collections.Immutable;
using System.Drawing;
using TestsBase;
using CC65.DebugDataParser.Models.CL65;
using CC65.DebugDataParser.Services.Implementation;
using NUnit.Framework;
using static System.Net.Mime.MediaTypeNames;

namespace CC65.DebugDataParser.Test.Services.Implementation;
public class CL65DebugSymbolsParserTest: BaseTest<CL65DebugSymbolsParser>
{
    [TestFixture]
    public class Split : CL65DebugSymbolsParserTest
    {
        [Test]
        public void GivenSampleData_SplitsCorrectly()
        {
            const string source = "id=1,name=\"text\",scope=0,type=0,sc=ext,sym=4";
            var actual = Target.Split(source);

            var actualTransformed = actual.Select(p => source[p.Start..p.End]);
            Assert.That(actualTransformed, Is.EquivalentTo(new string[] { "id=1", "name=\"text\"", "scope=0", "type=0", "sc=ext", "sym=4" }));
        }
        [Test]
        public void GivenOneValue_SplitsCorrectly()
        {
            const string source = "major=2";
            var actual = Target.Split(source, '=');

            var actualTransformed = actual.Select(p => source[p.Start..p.End]);
            Assert.That(actualTransformed, Is.EquivalentTo(new string[] { "major", "2" }));
        }
    }
    [TestFixture]
    public class CreateKeyValuePairs : CL65DebugSymbolsParserTest
    {
        [Test]
        public void GivenSample_ParsesCorrectly()
        {
            var parts = ImmutableArray<(int Start, int End)>.Empty
                .Add((0, 7))
                .Add((8, 15));
            var actual = Target.CreateKeyValuePairs("major=2,minor=0", parts);

            Assert.That(actual, Is.EqualTo(
                ImmutableDictionary<string, string>.Empty
                    .Add("major", "2")
                    .Add("minor", "0")));
        }
    }
    [TestFixture]
    public class ParseLine: CL65DebugSymbolsParserTest
    {
        [Test]
        public void WhenVersionLine_ReturnsCorrectVersionLine()
        {
            const string line = "version	major=2,minor=0";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(new VersionLine(2, 0)));
        }
        [Test]
        public void WhenInfoLine_ReturnsCorrectInfoLine()
        {
            const string line = "info	csym=3,file=705,lib=1,line=2435,mod=48,scope=3,seg=11,span=17,sym=11,type=2";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new InfoLine(3, 705, 1, 2435, 48, 3, 11, 17, 11, 2)));
        }
        [Test]
        public void WhenCSymLine_ReturnsCorrectCSymLine()
        {
            const string line = "csym	id=0,name=\"printf\",scope=0,type=0,sc=ext,sym=5";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new CSymLine(0, "printf", 0, 0, Sc.Ext, 5)));
        }
        [Test]
        public void WhenFileLine_ReturnsCorrectFileLine()
        {
            const string line = "file	id=0,name=\"out/hello.s\",size=1278,mtime=0x62D52AB6,mod=0";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new FileLine(0, "out/hello.s", 1278, 0x62D52AB6, 0)));
        }
        [Test]
        public void WhenLibLine_ReturnsCorrectLibLine()
        {
            const string line = "lib	id=0,name=\"D:\\Utilities\\cc65\\lib/c64.lib\"";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new LibLine(0, "D:\\Utilities\\cc65\\lib/c64.lib")));
        }
        [Test]
        public void WhenLineLineMinimal_ReturnsCorrectLineLine()
        {
            const string line = "line	id=5,file=0,line=0";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new LineLine(5, 0, 0)));
        }
        [Test]
        public void WhenLineLineWithTypeAndSpan_ReturnsCorrectLineLine()
        {
            const string line = "line	id=18,file=2,line=10,type=1,span=14";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new LineLine(18, 2, 10, 1, 14)));
        }
        [Test]
        public void WhenLineLineWithCount_ReturnsCorrectLineLine()
        {
            const string line = "line	id=86,file=12,line=23,count=1";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new LineLine(86, 12, 23, Count: 1)));
        }
        [Test]
        public void WhenMinimalModLine_ReturnsCorrectModLine()
        {
            const string line = "mod	id=0,name=\"hello.o\",file=0";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new ModLine(0, "hello.o", 0)));
        }
        [Test]
        public void WhenModLineWithLib_ReturnsCorrectModLine()
        {
            const string line = "mod	id=9,name=\"_seterrno.o\",file=21,lib=0";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new ModLine(9, "_seterrno.o", 21, 0)));
        }
        [Test]
        public void WhenFullSegLine_ReturnsCorrectSegLine()
        {
            const string line = "seg	id=0,name=\"CODE\",start=0x000840,size=0x0874,addrsize=absolute,type=ro,oname=\"a.out\",ooffs=65";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new SegLine(0, "CODE", 0x000840, 0x0874, AddrSize.Absolute, SegType.Ro, "a.out", 65)));
        }
        [Test]
        public void WhenMinimalSegLine_ReturnsCorrectSegLine()
        {
            const string line = "seg	id=2,name=\"BSS\",start=0x001237,size=0x0031,addrsize=absolute,type=rw";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new SegLine(2, "BSS", 0x001237, 0x0031, AddrSize.Absolute, SegType.Rw)));
        }
        [Test]
        public void WhenFullSpanLine_ReturnsCorrectSpanLine()
        {
            const string line = "span	id=0,seg=1,start=0,size=4,type=1";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new SpanLine(0, 1, 0, 4, 1)));
        }
        [Test]
        public void WhenMinimalSpanLine_ReturnsCorrectSpanLine()
        {
            const string line = "span	id=1,seg=0,start=0,size=2";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new SpanLine(1, 0, 0, 2)));
        }
        [Test]
        public void WhenFullScopeLine_ReturnsCorrectScopeLine()
        {
            const string line = "scope	id=1,name=\"_main\",mod=0,type=scope,size=27,parent=0,sym=3,span=15";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new ScopeLine(1, "_main", 0, 27, 15, ScopeType.Scope, 0, 3)));
        }
        [Test]
        public void WhenMinimalScopeLine_ReturnsCorrectScopeLine()
        {
            const string line = "scope	id=2,name=\"\",mod=1,size=13,span=16";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new ScopeLine(2, "", 1, 13, 16)));
        }
        [Test]
        public void WhenFullSymLine_ReturnsCorrectSymLine()
        {
            const string line = "sym	id=0,name=\"L0001\",addrsize=absolute,size=1,scope=1,def=10+18,ref=13,val=0x85A,seg=0,type=lab,exp=8";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new SymLine(0, "L0001", AddrSize.Absolute, 1, "10+18", "13", SymType.Lab,
                1, 0x85A, 0, 8)));
        }
        [Test]
        public void WhenMinimalSymLine_ReturnsCorrectSymLine()
        {
            const string line = "sym	id=5,name=\"_printf\",addrsize=absolute,scope=0,def=1,ref=4,type=imp";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new SymLine(5, "_printf", AddrSize.Absolute, 0, "1", "4", SymType.Imp)));
        }
        [Test]
        public void WhenTypeLine_ReturnsCorrectTypeLine()
        {
            const string line = "type	id=1,val=\"800420\"";

            var actual = Target.ParseLine(line);

            Assert.That(actual, Is.EqualTo(
                new TypeLine(1, "800420")));
        }
    }
}
