﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using NSubstitute;
using NUnit.Framework;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;
using TestsBase;

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
    //protected BreakpointsViewModel Target;
    protected PdbFile CreateSourceFile() => CreateSourceFile(source, PdbPath.CreateRelative("file.asm"));
    protected PdbFile CreateSourceFile(string source, PdbPath path)
    {
        var lines = source.Split("\n")
            .Select((l, i) => PdbLine.Create(path, i + 1, default, default, 0, default, l))
            .ToImmutableArray();
        var file = new PdbFile(path, default, lines);
        return file;
    }
    protected Pdb CreatePdb(PdbFile file)
    {
        var linesToFileMap = file.Lines.ToImmutableDictionary(l => l, l => file);
        return new Pdb( 
            ImmutableDictionary<PdbPath, PdbFile>.Empty.Add(file.Path, file),
            ImmutableDictionary<string, PdbLabel>.Empty,
            linesToFileMap,
            ImmutableDictionary<string, PdbVariable>.Empty, default,
            file.Lines,
            ImmutableDictionary<PdbLine, LineSymbolReferences>.Empty,
            PdbAddressToLineMap.Empty);
    }
    [SetUp]
    public new void SetUp()
    {
        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        var globals = new Globals(Substitute.For<ILogger<Globals>>(), Substitute.For<ISettingsManager>());
        fixture.Register(() => globals);
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
            var sourceLine = PdbLine.Create(PdbPath.Empty, 1, default, default, 0, default, text);
            var sourceFile = new PdbFile(PdbPath.CreateRelative("file.a"), default, ImmutableArray<PdbLine>.Empty.Add(sourceLine));
            // duplicates data
            //var newLine = sourceLine with { DataLength = 0 };
            var newLine = sourceLine with { };
            var lines = ImmutableArray<PdbLine>.Empty.Add(newLine);
            var newFile = sourceFile with { Lines = lines };
            var pdb = CreatePdb(newFile);

            var actual = BreakpointsViewModel.FindMatchingLine(pdb, sourceFile,
                new BreakpointsViewModel.LineSearchCriteria(sourceLine.LineNumber, sourceLine.Text));

            Assert.That(actual, Is.EqualTo((newFile, newLine)));
        }
        [Test]
        public void WhenLineContentIsEqualButFileDiffers_ReturnsNull()
        {
            const string text = "		beq +			; enter loop";
            var linesToFileMap = ImmutableDictionary.CreateBuilder<PdbLine, PdbFile>(ReferenceEqualityComparer.Instance);
            var sourceLine = PdbLine.Create(PdbPath.Empty, 1, default, default, 0, default, text);
            var sourceFile = new PdbFile(PdbPath.CreateRelative("file.a"), default, ImmutableArray<PdbLine>.Empty.Add(sourceLine));
            linesToFileMap.Add(sourceLine, sourceFile);
            // duplicates data
            //var newLine = sourceLine with { DataLength = 0 };
            var newLine = sourceLine with { };
            var lines = ImmutableArray<PdbLine>.Empty.Add(newLine);
            var newFile = sourceFile with { Path = PdbPath.CreateRelative("another_file.s"), Lines = lines };
            linesToFileMap.Add(newLine, newFile);
            var pdb = new Pdb(
                ImmutableDictionary<PdbPath, PdbFile>.Empty.Add(newFile.Path, newFile),
                ImmutableDictionary<string, PdbLabel>.Empty,
                linesToFileMap.ToImmutableDictionary(),
                ImmutableDictionary<string, PdbVariable>.Empty, default,
                lines,
                ImmutableDictionary<PdbLine, LineSymbolReferences>.Empty,
                PdbAddressToLineMap.Empty);

            var actual = BreakpointsViewModel.FindMatchingLine(pdb, sourceFile, 
                new BreakpointsViewModel.LineSearchCriteria(sourceLine.LineNumber, sourceLine.Text));

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
            var newFile = CreateSourceFile(modifiedSource, sourceFile.Path);
            var pdb = CreatePdb(newFile);

            // using 'jsr basout	; output character' as checkpoint
            var actual = BreakpointsViewModel.FindMatchingLine(pdb, sourceFile,
                new BreakpointsViewModel.LineSearchCriteria(sourceLine.LineNumber, sourceLine.Text));

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
            var newFile = CreateSourceFile(modifiedSource, sourceFile.Path);

            // using 'jsr basout	; output character' as checkpoint
            var actual = BreakpointsViewModel.FuzzyFindLine(5, 90,
                new BreakpointsViewModel.LineSearchCriteria(sourceLine.LineNumber, sourceLine.Text), newFile.Lines);

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
            var newFile = CreateSourceFile(modifiedSource, sourceFile.Path);

            // using 'jsr basout	; output character' as checkpoint
            var actual = BreakpointsViewModel.FuzzyFindLine(5, 90,
                new BreakpointsViewModel.LineSearchCriteria(sourceLine.LineNumber, sourceLine.Text), newFile.Lines);

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
            var newFile = CreateSourceFile(modifiedSource, sourceFile.Path);

            // using 'jsr basout	; output character' as checkpoint
            var actual = BreakpointsViewModel.FuzzyFindLine(5, 90,
                new BreakpointsViewModel.LineSearchCriteria(sourceLine.LineNumber, sourceLine.Text), newFile.Lines);

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
            var newFile = CreateSourceFile(modifiedSource, sourceFile.Path);

            // using 'jsr basout	; output character' as checkpoint
            var actual = BreakpointsViewModel.FuzzyFindLine(5, 90,
                new BreakpointsViewModel.LineSearchCriteria(sourceLine.LineNumber, sourceLine.Text), newFile.Lines);

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
    [TestFixture]
    public class UpdateBreakpointAsync: BreakpointsViewModelTest
    {
        class MockViceBridge : MockViceBridgeCore
        {
            protected override ViceResponse ProcessCommand<T>(T command)
            {
                return command switch
                {
                    CheckpointSetCommand checkpointSet =>
                        new CheckpointInfoResponse(default, ErrorCode.OK, 1, false, checkpointSet.StartAddress, checkpointSet.EndAddress,
                            checkpointSet.StopWhenHit, checkpointSet.Enabled, checkpointSet.CpuOperation, false, 0, 0, false),
                    ConditionSetCommand conditionSet => new EmptyViceResponse(default, ErrorCode.OK),
                    _ => throw new NotImplementedException()
                };
            }
        }
        [SetUp]
        public new void SetUp()
        {
            fixture.Register<IViceBridge>(() => new MockViceBridge());
        }
        [Test]
        public async Task WhenConditionChanges_AndBreakpointHasNoCheckpointNumber_ConditionIsUpdatedInSource()
        {
            var source = new BreakpointViewModel(
                stopWhenHit: true,
                isEnabled: true,
                BreakpointMode.Exec,
                bind: null,
                addressRanges: ImmutableHashSet<BreakpointAddressRange>.Empty.Add(new BreakpointAddressRange(0, 6)),
                condition: null);
            var clone = source.Clone();
            clone.Condition = "A == $20";

            await Target.UpdateBreakpointAsync(clone, source, default);

            Assert.That(source.Condition, Is.EqualTo("A == $20"));
        }
        [Test]
        public async Task WhenSourceHadErrors_UpdateClearsThem()
        {
            var source = new BreakpointViewModel(
                stopWhenHit: true,
                isEnabled: true,
                BreakpointMode.Exec,
                bind: null,
                addressRanges: ImmutableHashSet<BreakpointAddressRange>.Empty.Add(new BreakpointAddressRange(0, 6)),
                condition: null)
            {
                HasErrors = true,
                ErrorText = "Oops",
            };
            var clone = source.Clone();

            await Target.UpdateBreakpointAsync(clone, source, default);

            Assert.That(source.HasErrors, Is.False);
            Assert.That(source.ErrorText, Is.Null);
        }
    }
}
