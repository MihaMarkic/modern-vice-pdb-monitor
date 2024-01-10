using System.Collections.Immutable;
using AutoFixture;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using System.Threading;
using Modern.Vice.PdbMonitor.Engine.ViewModels;
using NSubstitute;
using NUnit.Framework;
using TestsBase;

namespace Modern.Vice.PdbMonitor.Engine.Test.ViewModels;
internal class CallStackViewModelTest: BaseTest<CallStackViewModel>
{
    const byte JSR = 0x20;
    const byte INITSP = 0xF4;
    [SetUp]
    public new void SetUp()
    {
        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        var globals = new Globals(Substitute.For<ILogger<Globals>>(), Substitute.For<ISettingsManager>());
        fixture.Register(() => globals);
    }
//F4 starting SP
//SP > $F2
//$01F3 82 (LO)
//$01F4 08 (HI)

//JSR at $0880
    [TestFixture]
    public class CreateCallStack: CallStackViewModelTest
    {
        [Test]
        public void WhenNoStack_CreatesEmptyCallStack()
        {
            var memory = ImmutableArray.Create(new byte[ushort.MaxValue+1]).AsSpan();

            Target.CreateCallStack(memory, INITSP);

            Assert.That(Target.CallStack, Is.Empty);
        }
        [Test]
        public void WhenSingleStackWithUnknownAddress_CreatesCallStackWithUnknownAddress()
        {
            var memory = ImmutableArray.Create(new byte[ushort.MaxValue+1]);
            memory = memory.SetItem(0x0880, JSR);
            memory = memory.SetItem(0x1F3, 0x82);
            memory = memory.SetItem(0x1F2, 0x80);
            byte sp = INITSP - 2;

            Target.CreateCallStack(memory.AsSpan(), sp);

            Assert.That(Target.CallStack, Is.Empty);
        }
    }
}
