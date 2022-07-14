using System.Collections.Immutable;
using System.Globalization;
using Modern.Vice.PdbMonitor.Converters;
using Modern.Vice.PdbMonitor.Engine.Models;
using NUnit.Framework;

namespace Modern.Vice.PdbMonitor.Test.Converters;

class FlagsConverterTest: BaseTest<FlagsConverter>
{
    [TestFixture]
    public class Convert: FlagsConverterTest
    {
        [Test]
        public void GivenSampleValue_ReturnsProperModels()
        {
            var actual = Target.Convert(0b00100101, typeof(ImmutableArray<FlagModel>), CultureInfo.InvariantCulture);

            Assert.That(actual, Is.EqualTo(ImmutableArray<FlagModel>.Empty
                .Add(new FlagModel("N", false))
                .Add(new FlagModel("V", false))
                .Add(new FlagModel("-", true))
                .Add(new FlagModel("B", false))
                .Add(new FlagModel("D", false))
                .Add(new FlagModel("I", true))
                .Add(new FlagModel("Z", false))
                .Add(new FlagModel("C", true))
                ));
        }
    }
}
