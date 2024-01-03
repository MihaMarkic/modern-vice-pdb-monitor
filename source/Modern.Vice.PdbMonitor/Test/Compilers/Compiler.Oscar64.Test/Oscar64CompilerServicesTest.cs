using System.Collections.Immutable;
using AutoFixture;
using Compiler.Oscar64.Models;
using Compiler.Oscar64.Services.Implementation;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Core.Services.Abstract;
using NSubstitute;
using NUnit.Framework;
using TestsBase;
using static Compiler.Oscar64.Oscar64CompilerServices;

namespace Compiler.Oscar64.Test;
internal class Oscar64CompilerServicesTest: BaseTest<Oscar64CompilerServices>
{
    async Task<DebugFile> LoadSampleAsync(string path, string name)
    {
        string fileName = Path.Combine("Samples", path, $"{name}.dbj");
        return await LoadSampleFromFileAsync(fileName);
    }
    async Task<DebugFile> LoadSampleAsync(string name)
    {
        string fileName = Path.Combine("Samples", $"{name}.dbj");
        return await LoadSampleFromFileAsync(fileName);
    }
    async Task<DebugFile> LoadSampleFromFileAsync(string fileName)
    {
        string content = File.ReadAllText(fileName);
        var parser = new Oscar64DbjParser(fixture.Create<ILogger<Oscar64DbjParser>>());
        return await parser.LoadContentAsync(content) ?? throw new NullReferenceException($"Failed to parse file {fileName}");
    }
    ImmutableArray<string> LoadLines(string path, string name)
    {
        string fileName = Path.Combine("Samples", path, name);
        return File.ReadAllLines(fileName).ToImmutableArray();
    }

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

            var actual = Oscar64CompilerServices.CreateLineVariables("projectDirectory", types, variables);

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
        const string RootDirectory = @"D:\ROOT";
        record Arguments(ImmutableArray<Function> Functions, ImmutableDictionary<PdbPath, PdbFile> EmptyPdbFiles, 
            ImmutableDictionary<string, PdbPath> Paths, ImmutableDictionary<int, PdbType> PdbTypes);

        /// <summary>
        /// From main.dbj and source files placed in <paramref name="sampleSubdirectory"/> directory creates
        /// all necessary arguments for Oscar64CompilerServices.CreatePdbLines method.
        /// </summary>
        /// <param name="projectDirectory"></param>
        /// <param name="sampleSubdirectory"></param>
        /// <param name="sourceFiles"></param>
        /// <returns></returns>
        async Task<Arguments> CreateArguments(string sampleSubdirectory, params string[] sourceFiles)
        {
            var dbj = await LoadSampleAsync(sampleSubdirectory, "main");

            var fileService = fixture.Freeze<IFileService>();
            var uniqueFileNames = ImmutableHashSet<string>.Empty;
            foreach (string sourceFile in sourceFiles)
            {
                string path = Path.Combine(RootDirectory, sourceFile);
                string file = sourceFile;
                fileService.ReadAllLines(path).Returns(LoadLines(sampleSubdirectory, file));
                uniqueFileNames = uniqueFileNames.Add(path);
            }

            var paths = uniqueFileNames.Select(n => new { Original = n, Path = PdbPath.Create(RootDirectory, n) })
                .ToImmutableDictionary(i => i.Original, i => i.Path);
            var emptyPdbFiles = uniqueFileNames.Select(n => PdbFile.CreateWithNoContent(paths[n]))
                .ToImmutableDictionary(f => f.Path, f => f);

            var pdbTypes = CreatePdbTypes(dbj.Types);

            return new Arguments(dbj.Functions, emptyPdbFiles, paths, pdbTypes);
        }
        [Test]
        public async Task WhenFunctionIsSplitBetweenTwoFiles_ParsesProperly()
        {
            var args = await CreateArguments("TestFunctionsSplitBetweenFiles", "character.cpp", "character.h");

            var (actual, variablesMap) = Target.CreatePdbLines(RootDirectory, args.Functions, args.EmptyPdbFiles, args.Paths, args.PdbTypes);

            Assert.That(actual.Count, Is.EqualTo(2));
        }
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
        /// <summary>
        /// Defines a sample variable as these are used merely for mapping and
        /// </summary>
        readonly Variable sampleVariable = new Variable("Sample", 0, 2, null, null, null, 0);
        [Test]
        public void WhenNoRanges_ReturnsNull()
        {
            var actual = Oscar64CompilerServices.GetLineVariable(0, ImmutableArray<VariableWithRange>.Empty);

            Assert.That(actual, Is.Null);
        }
        [Test]
        public void WhenOnlyAllRangePresent_ReturnsItsVariable()
        {
            var variable = new PdbVariable("variable", 0, 0, null, new PdbValueType(0, "type", 2, PdbVariableType.Int16), null);
            var ranges = ImmutableArray<VariableWithRange>.Empty
                .Add(new VariableWithRange(VariableRange.All, variable, sampleVariable));

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
            var variable = new PdbVariable("variable", 0, 0, null, new PdbValueType(0, "type", 2, PdbVariableType.Int16), null);
            var ranges = ImmutableArray<VariableWithRange>.Empty
                .Add(new VariableWithRange(new VariableRange(enter, leave), variable, sampleVariable));

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
            var variable = new PdbVariable("variable", 0, 0, null, new PdbValueType(0, "type", 2, PdbVariableType.Int16), null);
            var nestedVariable = new PdbVariable("variable", 0, 0, null, new PdbValueType(0, "type", 2, PdbVariableType.Int16), null);

            var ranges = ImmutableArray<VariableWithRange>.Empty
                .Add(new VariableWithRange(VariableRange.All, variable, sampleVariable))
                .Add(new VariableWithRange(new VariableRange(enter, leave), nestedVariable, sampleVariable));

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
            var variable = new PdbVariable("variable", 0, 0, null, new PdbValueType(0, "type", 2, PdbVariableType.Int16), null);
            var nestedVariable = new PdbVariable("variable", 0, 0, null, new PdbValueType(0, "type", 2, PdbVariableType.Int16), null);

            var ranges = ImmutableArray<VariableWithRange>.Empty
                .Add(new VariableWithRange(new VariableRange(0, 5), variable, sampleVariable))
                .Add(new VariableWithRange(new VariableRange(enter, leave), nestedVariable, sampleVariable));

            var actual = Oscar64CompilerServices.GetLineVariable(lineNumber, ranges);

            return actual == nestedVariable;
        }
    }

    [TestFixture]
    public class VariableRangeTest : Oscar64CompilerServicesTest
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
    [TestFixture]
    public class CreateEnumType : Oscar64CompilerServicesTest
    {
        [Test]
        public void WhenDuplicateEnumValues_GroupsThem()
        {
            var source = new Oscar64EnumType("WithDuplicates", 0, 1, null,
                ImmutableArray<Oscar64EnumMember>.Empty
                    .Add(new Oscar64EnumMember("One", 1))
                    .Add(new Oscar64EnumMember("Two", 2))
                    .Add(new Oscar64EnumMember("AnotherOne", 1)));

            var actual = Oscar64CompilerServices.CreateEnumType(source);

            Assert.That(actual.ByKey, Is.EquivalentTo(
                new Dictionary<object, string> {
                    { 1, "One, AnotherOne" },
                    { 2, "Two" }
                }));
        }
    }
}
