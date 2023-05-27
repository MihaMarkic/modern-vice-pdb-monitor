using System.Collections.Immutable;
using Compiler.Oscar64.Models;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Utils.Test;
using NUnit.Framework;
using static Compiler.Oscar64.Oscar64CompilerServices;

namespace Compiler.Oscar64.Test;
internal class Oscar64CompilerServicesTest: BaseTest<Oscar64CompilerServices>
{
    [TestFixture]
    public class CreateLineVariables : Oscar64CompilerServicesTest
    {
        [Test]
        public void WhenSimpleNestedVariables_AssignsNestedCorrectlyToTheirLines()
        {
            var types = ImmutableDictionary<int, PdbType>.Empty
                .Add(0, new PdbValueType(0, "null", 0, PdbVariableType.Void));
            var variables = ImmutableArray<Variable>.Empty
                .Add(new Variable("i", 0, 100, null, null, null, 0))
                .Add(new Variable("i", 0, 100, null, 5, 10, 0));

            var actual = Oscar64CompilerServices.CreateLineVariables(types, variables);

            Assert.That(actual.Count, Is.EqualTo(1));
            var ranges = actual["i"];
            Assert.That(ranges.Count, Is.EqualTo(2));
            Assert.That(ranges.Select(r => r.Range),
                Is.EquivalentTo(new VariableRange[] { VariableRange.All, new VariableRange(5, 10) }));
        }
    }
    [TestFixture]
    public class CreatePdbLines : Oscar64CompilerServicesTest
    {
        //const string File = "file.c";
        //protected int[] lineNumbers = default!;
        //DebugFile CreateSample()
        //{
        //    var variables = ImmutableArray<Variable>.Empty
        //        .Add(new Variable("global", 0, 0, null, null, null, 0))
        //        .Add(new Variable("ranged", 0, 0, null, 2, 6, 0))
        //        .Add(new Variable("outer_i", 0, 0, null, 2, 9, 0))
        //        .Add(new Variable("inner_i", 0, 0, null, 4, 7, 0))
        //        .Add(new Variable("global_2", 0, 0, null, null, null, 0))
        //        .Add(new Variable("nested_global_2", 0, 0, null, 2, 7, 0));
        //    lineNumbers = new int[] { 1, 2, 4, 6, 7, 9, 10 };
        //    var lines = lineNumbers.Select(n => new FunctionLine(0, 0, File, n)).ToImmutableArray();
        //    var functions = ImmutableArray<Function>.Empty
        //        .Add(new Function("test", 0, 0, 0, File, 0, lines, variables));
        //    var types = ImmutableArray<Oscar64Type>.Empty
        //        .Add(new Oscar64UIntType("type", 0, 2, null));
        //    return new DebugFile(ImmutableArray<MemoryBlock>.Empty, ImmutableArray<Variable>.Empty, functions, types);
        //}
        //[Test]
        //public void WhenNestedVariables_LineWithGlobalVariableHasGlobal()
        //{
        //    var sample = CreateSample();

        //    var pdbTypes = ImmutableDictionary<int, PdbType>.Empty
        //        .Add(0, new PdbValueType(0, "type", 2, PdbVariableType.UInt16));
        //    var types = sample.Types.ToImmutableDictionary(t => t.TypeId, t => t);
        //    var path = PdbPath.CreateRelative(File);
        //    var paths = ImmutableDictionary<PdbPath, PdbFile>.Empty
        //        .Add(path, new PdbFile(path, ImmutableDictionary<string, PdbFunction>.Empty, ImmutableArray<PdbLine>.Empty));
        //    var fileService = fixture.Freeze<IFileService>();
        //    fileService.ReadAllLines(default!).ReturnsForAnyArgs(
        //        Enumerable.Range(0, lineNumbers.Max()).Select(n => $"Line {n+1}").ToImmutableArray()
        //        );

        //    var actual = Target.CreatePdbLines("", 
        //        sample.Functions, 
        //        ImmutableDictionary<PdbPath, PdbFile>.Empty
        //            .Add(path, PdbFile.CreateFromRelativePath(File)),
        //        ImmutableDictionary<string, PdbPath>.Empty
        //            .Add(File, path), 
        //        pdbTypes);

        //}
    }
    [TestFixture]
    public class GetLineVariables : Oscar64CompilerServicesTest
    {
        [Test]
        public void WhenNoVariables_ReturnsEmptyResult()
        {
            var actual = Target.GetLineVariables(0,
                ImmutableDictionary<string, ImmutableArray<VariableWithRange>>.Empty);

            Assert.That(actual, Is.Empty);
        }
    }
    [TestFixture]
    public class GetLineVariable : Oscar64CompilerServicesTest
    {
        [Test]
        public void WhenNoRanges_ReturnsNull()
        {
            var actual = Oscar64CompilerServices.GetLineVariable(0, ImmutableArray<VariableWithRange>.Empty);

            Assert.That(actual, Is.Null);
        }
        [Test]
        public void WhenOnlyAllRangePresent_ReturnsItsVariable()
        {
            var variable = new PdbVariable("variable", 0, 0, null, new PdbValueType(0, "type", 2, PdbVariableType.Int16));
            var ranges = ImmutableArray<VariableWithRange>.Empty
                .Add(new VariableWithRange(VariableRange.All, variable));

            var actual = Oscar64CompilerServices.GetLineVariable(0, ranges);

            Assert.That(actual, Is.Not.Null);
        }
        [TestCase(1, 3, 0, ExpectedResult = false)]
        [TestCase(1, 3, 1, ExpectedResult = true)]
        [TestCase(1, 3, 2, ExpectedResult = true)]
        [TestCase(1, 3, 3, ExpectedResult = true)]
        [TestCase(1, 3, 4, ExpectedResult = false)]
        public bool WhenRangePresent_AndLineIsWithinRange_ReturnsVariablePresence(
            int enter, int leave, int lineNumber)
        {
            var variable = new PdbVariable("variable", 0, 0, null, new PdbValueType(0, "type", 2, PdbVariableType.Int16));
            var ranges = ImmutableArray<VariableWithRange>.Empty
                .Add(new VariableWithRange(new VariableRange(enter, leave), variable));

            var actual = Oscar64CompilerServices.GetLineVariable(lineNumber, ranges);

            return actual is not null;
        }
        [TestCase(1, 3, 0, ExpectedResult = false)]
        [TestCase(1, 3, 1, ExpectedResult = true)]
        [TestCase(1, 3, 2, ExpectedResult = true)]
        [TestCase(1, 3, 3, ExpectedResult = true)]
        [TestCase(1, 3, 4, ExpectedResult = false)]
        public bool WhenRangePresent_AndLineIsWithinRange_AndVariableIsNestedWithinAll_ReturnsNestedVariablePresence(
            int enter, int leave, int lineNumber)
        {
            var variable = new PdbVariable("variable", 0, 0, null, new PdbValueType(0, "type", 2, PdbVariableType.Int16));
            var nestedVariable = new PdbVariable("variable", 0, 0, null, new PdbValueType(0, "type", 2, PdbVariableType.Int16));

            var ranges = ImmutableArray<VariableWithRange>.Empty
                .Add(new VariableWithRange(VariableRange.All, variable))
                .Add(new VariableWithRange(new VariableRange(enter, leave), nestedVariable));

            var actual = Oscar64CompilerServices.GetLineVariable(lineNumber, ranges);

            return actual == nestedVariable;
        }
        [TestCase(1, 3, 0, ExpectedResult = false)]
        [TestCase(1, 3, 1, ExpectedResult = true)]
        [TestCase(1, 3, 2, ExpectedResult = true)]
        [TestCase(1, 3, 3, ExpectedResult = true)]
        [TestCase(1, 3, 4, ExpectedResult = false)]
        public bool WhenRangePresent_AndLineIsWithinRange_AndVariableIsNestedWithinNestedRange_ReturnsNestedVariablePresence(
            int enter, int leave, int lineNumber)
        {
            var variable = new PdbVariable("variable", 0, 0, null, new PdbValueType(0, "type", 2, PdbVariableType.Int16));
            var nestedVariable = new PdbVariable("variable", 0, 0, null, new PdbValueType(0, "type", 2, PdbVariableType.Int16));

            var ranges = ImmutableArray<VariableWithRange>.Empty
                .Add(new VariableWithRange(new VariableRange(0, 5), variable))
                .Add(new VariableWithRange(new VariableRange(enter, leave), nestedVariable));

            var actual = Oscar64CompilerServices.GetLineVariable(lineNumber, ranges);

            return actual == nestedVariable;
        }
    }

    [TestFixture]
    public class VariableRangeTest
    {
        [TestFixture]
        public class Contains: VariableRangeTest
        {
            [TestCase(null, null, 1, ExpectedResult = true)]
            [TestCase(1, 3, 0, ExpectedResult = false)]
            [TestCase(1, 3, 1, ExpectedResult = true)]
            [TestCase(1, 3, 2, ExpectedResult = true)]
            [TestCase(1, 3, 3, ExpectedResult = true)]
            [TestCase(1, 3, 4, ExpectedResult = false)]
            public bool GivenCases_ReturnsCorrectValue(int? start, int? end, int value)
            {
                return new VariableRange(start, end).Contains(value);
            }
        }
    }
}
