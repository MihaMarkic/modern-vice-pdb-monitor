using Modern.Vice.PdbMonitor.Engine.ViewModels;
using NUnit.Framework;
using TestsBase;

namespace Modern.Vice.PdbMonitor.Engine.Test.ViewModels;
internal class SourceFileViewModelTest : BaseTest<SourceFileViewModel>
{
}

internal class LineViewModelTest: BaseTest<LineViewModel>
{
    [TestFixture]
    public class CreateSpacePrefix: LineViewModelTest
    {
        [TestCase("", ExpectedResult = "")]
        [TestCase(" ", ExpectedResult = " ")]
        [TestCase("\t", ExpectedResult = "\t")]
        [TestCase("\t ", ExpectedResult = "\t ")]
        [TestCase(" \t ", ExpectedResult = " \t ")]
        [TestCase("a", ExpectedResult = "")]
        [TestCase(" a", ExpectedResult = " ")]
        [TestCase("\ta", ExpectedResult = "\t")]
        [TestCase("\t a", ExpectedResult = "\t ")]
        [TestCase(" \t a", ExpectedResult = " \t ")]
        public string GivenInput_ReturnsProperPrefix(string content)
        {
            return LineViewModel.CreateSpacePrefix(content);
        }
    }
}
