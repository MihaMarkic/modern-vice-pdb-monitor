using Modern.Vice.PdbMonitor.Engine.ViewModels;
using NUnit.Framework;
using TestsBase;

namespace Modern.Vice.PdbMonitor.Engine.Test.ViewModels;
internal class MessagesHistoryViewModelTest: BaseTest<MessagesHistoryViewModel>
{
    [TestFixture]
    public class Clear: MessagesHistoryViewModelTest
    {
        [Test]
        public void WhenMessagesHistoryDoesNotImplementIMessageHistorySource_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Target.Clear());
        }
    }
}
