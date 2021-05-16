using System.Diagnostics;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NUnit.Framework;

namespace Modern.Vice.PdbMonitor.Test
{
    public abstract class BaseTest<T>
        where T : class
    {
        protected Fixture fixture;
        T target;
        public T Target
        {
            [DebuggerStepThrough]
            get
            {
                if (target is null)
                {
                    target = fixture.Build<T>().OmitAutoProperties().Create();
                }
                return target;
            }
        }

        [SetUp]
        public virtual void SetUp()
        {
            fixture = new Fixture();
            fixture.Customize(new AutoNSubstituteCustomization());
        }
        [TearDown]
        public void TearDown()
        {
            target = null;
        }
    }
}
