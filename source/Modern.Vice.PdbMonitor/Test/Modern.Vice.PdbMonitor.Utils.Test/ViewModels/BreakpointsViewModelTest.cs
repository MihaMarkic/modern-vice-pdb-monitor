using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using Modern.Vice.PdbMonitor.Utils.Test;
using NUnit.Framework;
using System.Collections.Immutable;
using System.Linq;

namespace Modern.Vice.PdbMonitor.Engine.Test.ViewModels;

class BreakpointsViewModelTest: BaseTest<BreakpointsViewModel>
{
    const string source =
@"start	ldx #0
		lda .string
		beq +			; enter loop

-			jsr basout	; output character
			inx		; advance pointer
+			lda .string, x	; get character
			bne -		; check whether last
		rts";

    protected PdbFile CreateSourceFile() => CreateSourceFile(source, "file.asm");
    protected PdbFile CreateSourceFile(string source, string fileName)
    {
        var lines = source.Split("\n")
            .Select((l, i) => new PdbLine(fileName, i + 1, default, default, 0, default, l))
            .ToImmutableArray();
        var file = new PdbFile(fileName, lines);
        return file;
    }
    protected Pdb CreatePdb(PdbFile file)
    {
        return new Pdb(file.Lines, ImmutableDictionary<string, PdbFile>.Empty.Add(file.RelativePath, file),
            ImmutableDictionary<string, PdbLabel>.Empty, file.Lines);
    }
    [TestFixture]
    public class FindMatchingLine: BreakpointsViewModelTest
    {
        [Test]
        public void WhenPdbIsNull_ReturnsNull()
        {
            var actual = BreakpointsViewModel.FindMatchingLine(null, default, default);

            Assert.That(actual, Is.Null);
        }
        [Test]
        public void WhenLineContentIsEqual_ReturnsNewLine()
        {
            const string text = "		beq +			; enter loop";
            var sourceLine = new PdbLine("file.a", 1, default, default, 0, default, text);
            var sourceFile = new PdbFile("file.a", ImmutableArray<PdbLine>.Empty.Add(sourceLine));
            // duplicates data
            var newLine = sourceLine with { DataLength = 0 };
            var lines = ImmutableArray<PdbLine>.Empty.Add(newLine);
            var newFile = sourceFile with { Lines = lines };
            var pdb = CreatePdb(newFile);

            var actual = BreakpointsViewModel.FindMatchingLine(pdb, sourceFile, sourceLine);

            Assert.That(actual, Is.EqualTo((newFile, newLine)));
        }
        [Test]
        public void WhenLineContentIsEqualButFileDiffers_ReturnsNull()
        {
            const string text = "		beq +			; enter loop";
            var sourceLine = new PdbLine("file.a", 1, default, default, 0, default, text);
            var sourceFile = new PdbFile("file.a", ImmutableArray<PdbLine>.Empty.Add(sourceLine));
            // duplicates data
            var newLine = sourceLine with { DataLength = 0 };
            var lines = ImmutableArray<PdbLine>.Empty.Add(newLine);
            var newFile = sourceFile with { RelativePath = "another_file.s", Lines = lines };
            var pdb = new Pdb(lines, ImmutableDictionary<string, PdbFile>.Empty.Add(newFile.RelativePath, newFile),
                ImmutableDictionary<string, PdbLabel>.Empty, lines);

            var actual = BreakpointsViewModel.FindMatchingLine(pdb, sourceFile, sourceLine);

            Assert.That(actual, Is.Null);
        }
        [Test]
        public void WhenOriginalLineIsMovedDownOneLine_ReturnsNewLine()
        {
            const string modifiedSource =
@"start	ldx #0
		lda .string
		beq +			; enter loop

        ; NEW LINE
-			jsr basout	; output character
			inx		; advance pointer
+			lda .string, x	; get character
			bne -		; check whether last
		rts";

            var sourceFile = CreateSourceFile();
            var sourceLine = sourceFile.Lines[4];
            var newFile = CreateSourceFile(modifiedSource, sourceFile.RelativePath);
            var pdb = CreatePdb(newFile);

            // using 'jsr basout	; output character' as checkpoint
            var actual = BreakpointsViewModel.FindMatchingLine(pdb, sourceFile, sourceLine);

            Assert.That(actual.Value.Line, Is.SameAs(newFile.Lines[5]));
        }
    }
    [TestFixture]
    public class FuzzyFindLine : BreakpointsViewModelTest
    {
        [Test]
        public void WhenOriginalLineIsMovedDownOneLine_ReturnsNewLine()
        {
            const string modifiedSource =
@"start	ldx #0
		lda .string
		beq +			; enter loop

        ; NEW LINE
-			jsr basout	; output character
			inx		; advance pointer
+			lda .string, x	; get character
			bne -		; check whether last
		rts";

            var sourceFile = CreateSourceFile();
            var sourceLine = sourceFile.Lines[4];
            var newFile = CreateSourceFile(modifiedSource, sourceFile.RelativePath);

            // using 'jsr basout	; output character' as checkpoint
            var actual = BreakpointsViewModel.FuzzyFindLine(5, 90, sourceLine, newFile.Lines);

            Assert.That(actual, Is.SameAs(newFile.Lines[5]));
        }
        [Test]
        public void WhenOriginalLineIsSlightlyModified_ReturnsNewLine()
        {
            const string modifiedSource =
@"start	ldx #0
		lda .string
		beq +			; enter loop

-			jsr basout	; output character xxx
			inx		; advance pointer
+			lda .string, x	; get character
			bne -		; check whether last
		rts";

            var sourceFile = CreateSourceFile();
            var sourceLine = sourceFile.Lines[4];
            var newFile = CreateSourceFile(modifiedSource, sourceFile.RelativePath);

            // using 'jsr basout	; output character' as checkpoint
            var actual = BreakpointsViewModel.FuzzyFindLine(5, 90, sourceLine, newFile.Lines);

            Assert.That(actual, Is.SameAs(newFile.Lines[4]));
        }
        [Test]
        public void WhenOriginalLineIsSlightlyModifiedAndMovedDownOneLine_ReturnsNewLine()
        {
            const string modifiedSource =
@"start	ldx #0
		lda .string
		beq +			; enter loop

        ; NEW LINE
-			jsr basout	; output character xxx
			inx		; advance pointer
+			lda .string, x	; get character
			bne -		; check whether last
		rts";

            var sourceFile = CreateSourceFile();
            var sourceLine = sourceFile.Lines[4];
            var newFile = CreateSourceFile(modifiedSource, sourceFile.RelativePath);

            // using 'jsr basout	; output character' as checkpoint
            var actual = BreakpointsViewModel.FuzzyFindLine(5, 90, sourceLine, newFile.Lines);

            Assert.That(actual, Is.SameAs(newFile.Lines[5]));
        }
        [Test]
        public void WhenOriginalLineIsSlightlyModifiedAndMovedUp_ReturnsNewLine()
        {
            const string modifiedSource =
@"start	ldx #0
		lda .string
		beq +			; enter loop
-			jsr basout	; output character xxx
			inx		; advance pointer
+			lda .string, x	; get character
			bne -		; check whether last
		rts";

            var sourceFile = CreateSourceFile();
            var sourceLine = sourceFile.Lines[4];
            var newFile = CreateSourceFile(modifiedSource, sourceFile.RelativePath);

            // using 'jsr basout	; output character' as checkpoint
            var actual = BreakpointsViewModel.FuzzyFindLine(5, 90, sourceLine, newFile.Lines);

            Assert.That(actual, Is.SameAs(newFile.Lines[3]));
        }
    }
    [TestFixture]
    public class GetCandidatesForFuzzySearch : BreakpointsViewModelTest
    {
        [TestCase(0, 3, ExpectedResult = new int[] { 1, 2, 3})]
        [TestCase(8, 3, ExpectedResult = new int[] { 7, 6, 5 })]
        [TestCase(7, 3, ExpectedResult = new int[] { 8, 6, 5, 4 })]
        [TestCase(5, 3, ExpectedResult = new int[] { 6, 4, 7, 3, 8, 2 })]
        public int[] GivenSample_ReturnsExpectedOrderOfLines(int lineIndex, int maxDelta)
        {
            var sourceFile = CreateSourceFile();

            var actual = BreakpointsViewModel.GetCandidatesForFuzzySearch(lineIndex, maxDelta, sourceFile.Lines);

            return actual.Select(l => l.LineNumber - 1).ToArray();
        }
    }
    [TestFixture]
    public class IsFuzzyMatch: BreakpointsViewModelTest
    {
        [Test]
        public void WhenExactMatch_ReturnsScoreOf100()
        {
            var actual = BreakpointsViewModel.GetFuzzyMatchScore("		lda .string", "		lda .string");

            Assert.That(actual, Is.EqualTo(100));
        }
    }
}
