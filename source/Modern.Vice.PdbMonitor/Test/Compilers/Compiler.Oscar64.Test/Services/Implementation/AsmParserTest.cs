using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Compiler.Oscar64.Models;
using Compiler.Oscar64.Services.Implementation;
using NUnit.Framework;
using TestsBase;

namespace Compiler.Oscar64.Test.Services.Implementation;
internal class AsmParserTest: BaseTest<AsmParser>
{
    [TestFixture]
    public class CreateLabelLine: AsmParserTest
    {
        [Test]
        public void GivenSampleLabelLine_ParsesCorrectly()
        {
            const string Source = ".s0:";

            var actual = Target.CreateLabelLine(Source);

            Assert.That(actual!.Name, Is.EqualTo("s0"));
        }
    }
    [TestFixture]
    public class CreateDataLine : AsmParserTest
    {
        [Test]
        public void GivenSampleLine_ReturnsParser()
        {
            var actual = Target.CreateDataLine("08ea : 85 1b __ STA ACCU + 0 ", 0x08ea);

            Assert.That(actual!.Address, Is.EqualTo(0x08ea));
            Assert.That(actual!.Text, Is.EqualTo("STA ACCU + 0"));
            Assert.That(actual!.Content, Is.EquivalentTo(ImmutableArray.Create<byte>([0x85, 0x1B])));
        }
    }
    [TestFixture]
    public class CreateSourceLine : AsmParserTest
    {
        [Test]
        public void GivenSampleLine_ParsesCorrectly()
        {
            var actual = Target.CreateSourceLine(";   6, \"D:/Temp/oscar64/memory/main.cpp\"");

            Assert.That(actual!.LineNumber, Is.EqualTo(6));
            Assert.That(actual.FilePath, Is.EqualTo("D:/Temp/oscar64/memory/main.cpp"));
        }
    }
    [TestFixture]
    public class CreateFunctionLine : AsmParserTest
    {
        [Test]
        public void WhenLineWithoutFullFunctionName_ReturnsNull()
        {
            var actual = Target.CreateFunctionLine("spentry:");

            Assert.That(actual, Is.Null);
        }
        [Test]
        public void WhenLineWithFullFunctionName_ReturnsParsed()
        {
            var actual = Target.CreateFunctionLine("main: ; main()->i16");

            Assert.That(actual!.Name, Is.EqualTo("main"));
            Assert.That(actual!.FullName, Is.EqualTo("main()->i16"));
        }
    }
    [TestFixture]
    public class CreateVariableLine : AsmParserTest
    {
        [Test]
        public void TeGivenSampleLine_ParsesCorrectly()
        {
            var actual = Target.CreateVariableLine("test:");

            Assert.That(actual!.Name, Is.EqualTo("test"));
        }
    }
    [TestFixture]
    public class Parse : AsmParserTest
    {
        static IList<string> LoadSample(string name)
        {
            return File.ReadAllLines(Path.Combine("Samples", $"{name}.asm"));
        }
        [Test]
        public void GivenSampleWithEmptyFunction_ParsesCorrectly()
        {
            const string Text = """
                --------------------------------------------------------------------
                main: ; main()->i16
                """;

            var actual = Target.Parse(Text.Split('\n').ToImmutableArray());

            Func<AssemblyFunction, AssemblyFunction, bool> comparer = (a, b) =>
            {
                return a.Name.Equals(b.Name) && a.FullName.Equals(b.FullName);
            };
            var expected = ImmutableArray.Create([
                new AssemblyFunction("main", "main()->i16", [])
            ]);

            Assert.That(actual, Is.EquivalentTo(expected).Using(AssemblyFunctionComparer.Default));
        }
        [Test]
        public void GivenSampleWithSimpleFunction_ParsesCorrectly()
        {
            const string Text = """
                --------------------------------------------------------------------
                main: ; main()->i16
                .s0:
                ;  24, "D:/Temp/oscar64/memory/main.cpp"
                0880 : a9 03 __ LDA #$03
                """;

            var actual = Target.Parse(Text.Split('\n').ToImmutableArray());

            Func<AssemblyFunction, AssemblyFunction, bool> comparer = (a, b) =>
            {
                return a.Name.Equals(b.Name) && a.FullName.Equals(b.FullName);
            };
            var expected = ImmutableArray.Create([
                new AssemblyFunction("main", "main()->i16",
                    [
                        new AssemblySourceLine(24, "D:/Temp/oscar64/memory/main.cpp", 
                            [
                                new AssemblyExecutionLine(0x880, "LDA #$03", [0xa9, 0x03])
                            ])
                    ])
            ]);

            Assert.That(actual, Is.EquivalentTo(expected).Using(AssemblyFunctionComparer.Default));
        }
        [Test]
        public void GivenSampleWithSimpleTwoFunctions_ParsesCorrectly()
        {
            const string Text = """
                --------------------------------------------------------------------
                main: ; main()->i16
                .s0:
                ;  24, "D:/Temp/oscar64/memory/main.cpp"
                0880 : a9 03 __ LDA #$03
                --------------------------------------------------------------------
                sample_function: ; sample_function(i16)->i16
                .s0:
                ;   8, "D:/Temp/oscar64/memory/main.cpp"
                08f9 : a9 05 __ LDA #$05
                """;

            var actual = Target.Parse(Text.Split('\n').ToImmutableArray());

            Func<AssemblyFunction, AssemblyFunction, bool> comparer = (a, b) =>
            {
                return a.Name.Equals(b.Name) && a.FullName.Equals(b.FullName);
            };
            var expected = ImmutableArray.Create([
                new AssemblyFunction("main", "main()->i16",
                    [
                        new AssemblySourceLine(24, "D:/Temp/oscar64/memory/main.cpp",
                            [
                                new AssemblyExecutionLine(0x880, "LDA #$03", [0xa9, 0x03])
                            ])
                    ]),
                new AssemblyFunction("sample_function", "sample_function(i16)->i16",
                    [
                        new AssemblySourceLine(8, "D:/Temp/oscar64/memory/main.cpp",
                            [
                                new AssemblyExecutionLine(0x8f9, "LDA #$05", [0xa9, 0x05])
                            ])
                    ])
            ]);

            Assert.That(actual, Is.EquivalentTo(expected).Using(AssemblyFunctionComparer.Default));
        }
        [Test]
        public void GivenSampleWithSimpleTwoFunctionsAndOneDoesNotHaveSourceLines_ParsesCorrectly()
        {
            const string Text = """
                --------------------------------------------------------------------
                main: ; main()->i16
                0880 : a9 03 __ LDA #$03
                --------------------------------------------------------------------
                sample_function: ; sample_function(i16)->i16
                .s0:
                ;   8, "D:/Temp/oscar64/memory/main.cpp"
                08f9 : a9 05 __ LDA #$05
                """;

            var actual = Target.Parse(Text.Split('\n').ToImmutableArray());

            Func<AssemblyFunction, AssemblyFunction, bool> comparer = (a, b) =>
            {
                return a.Name.Equals(b.Name) && a.FullName.Equals(b.FullName);
            };
            var expected = ImmutableArray.Create([
                new AssemblyFunction("main", "main()->i16",[]),
                new AssemblyFunction("sample_function", "sample_function(i16)->i16",
                    [
                        new AssemblySourceLine(8, "D:/Temp/oscar64/memory/main.cpp",
                            [
                                new AssemblyExecutionLine(0x8f9, "LDA #$05", [0xa9, 0x05])
                            ])
                    ])
            ]);

            Assert.That(actual, Is.EquivalentTo(expected).Using(AssemblyFunctionComparer.Default));
        }
        [Test]
        public void GivenSampleWithSimpleFunctionWithTwoSourceLines_ParsesCorrectly()
        {
            const string Text = """
        --------------------------------------------------------------------
        main: ; main()->i16
        .s0:
        ;  24, "D:/Temp/oscar64/memory/main.cpp"
        0880 : a9 03 __ LDA #$03
        ;  15, "D:/Temp/oscar64/memory/main.cpp"
        08a0 : 91 43 __ STA (T0 + 0),y 
        """;

            var actual = Target.Parse(Text.Split('\n').ToImmutableArray());

            Func<AssemblyFunction, AssemblyFunction, bool> comparer = (a, b) =>
            {
                return a.Name.Equals(b.Name) && a.FullName.Equals(b.FullName);
            };
            var expected = ImmutableArray.Create([
                new AssemblyFunction("main", "main()->i16",
                    [
                        new AssemblySourceLine(24, "D:/Temp/oscar64/memory/main.cpp",
                            [
                                new AssemblyExecutionLine(0x880, "LDA #$03", [0xa9, 0x03])
                            ]),
                        new AssemblySourceLine(15, "D:/Temp/oscar64/memory/main.cpp",
                        [
                            new AssemblyExecutionLine(0x8a0, "STA (T0 + 0),y", [0x91, 0x43])
                        ])
                    ])
            ]);

            Assert.That(actual, Is.EquivalentTo(expected).Using(AssemblyFunctionComparer.Default));
        }
    }
}
public class AssemblyFunctionComparer : IEqualityComparer<AssemblyFunction>
{
    public readonly static AssemblyFunctionComparer Default = new AssemblyFunctionComparer();
    public bool Equals(AssemblyFunction? x, AssemblyFunction? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        else if (x is null || y is null)
        {
            return false;
        }
        else
        {
            return x.Name.Equals(y.Name) && x.FullName.Equals(y.FullName)
                && x.SourceLines.AreEquals(y.SourceLines, AssemblySourceLineComparer.Default);
        }
    }

    public int GetHashCode([DisallowNull] AssemblyFunction obj)
    {
        int hashCode = obj.GetHashCode();
        foreach (var l in obj.SourceLines)
        {
            hashCode = HashCode.Combine(hashCode, l);
        }
        return hashCode;
    }
}
public class AssemblyExecutionLineComparer : IEqualityComparer<AssemblyExecutionLine>
{
    public readonly static AssemblyExecutionLineComparer Default = new AssemblyExecutionLineComparer();
    public bool Equals(AssemblyExecutionLine? x, AssemblyExecutionLine? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        else if (x is null || y is null)
        {
            return false;
        }
        else
        {
            return x.Address == y.Address && x.Text.Equals(y.Text) && x.Data.AreEquals(y.Data);
        }
    }

    public int GetHashCode([DisallowNull] AssemblyExecutionLine obj)
    {
        int hashCode = obj.GetHashCode();
        foreach (byte b in obj.Data)
        {
            hashCode = HashCode.Combine(hashCode, b);
        }
        return hashCode;
    }
}
public class AssemblySourceLineComparer : IEqualityComparer<AssemblySourceLine>
{
    public readonly static AssemblySourceLineComparer Default = new AssemblySourceLineComparer();
    public bool Equals(AssemblySourceLine? x, AssemblySourceLine? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        else if (x is null || y is null)
        {
            return false;
        }
        else
        {
            return x.ExecutionLines.AreEquals(y.ExecutionLines, AssemblyExecutionLineComparer.Default);
        }
    }

    public int GetHashCode([DisallowNull] AssemblySourceLine obj)
    {
        int hash = obj.GetHashCode();
        foreach (var l in obj.ExecutionLines)
        {
            hash = HashCode.Combine(hash, l.GetHashCode());
        }
        return hash;
    }
}
