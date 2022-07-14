using System;
using NUnit.Framework;

namespace Modern.Vice.PdbMonitor.Engine.Test.Extensions;

class StringExtensionsTest
{
    [TestFixture]
    public class CalculateRequiredSpacesForTab: StringExtensionsTest
    {
        const int TabWidth = 4;
        [TestCase(0, ExpectedResult = 4)]
        [TestCase(3, ExpectedResult = 1)]
        [TestCase(4, ExpectedResult = 4)]
        public int GivenArguments_CalculatesProperly(int position)
        {
            return StringExtension.CalculateRequiredSpacesForTab(position, TabWidth);
        }
    }
}
