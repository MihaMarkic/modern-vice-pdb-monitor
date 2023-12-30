using System.Diagnostics;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NUnit.Framework;

namespace TestsBase;

public abstract class BaseTest<T>
    where T : class
{
    protected Fixture fixture = default!;
    T target = default!;
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
    public void SetUp()
    {
        fixture = new Fixture();
        fixture.Customize(new AutoNSubstituteCustomization());
    }
    [TearDown]
    public void TearDown()
    {
        target = null!;
    }
}
