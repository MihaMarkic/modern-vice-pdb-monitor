using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Modern.Vice.PdbMonitor.Core.Test;

public class CommandsManagerTest
{
    CommandsManagerTestOwner owner = null!;
    CommandsManager target = null!;
    [SetUp]
    public void SetUp()
    {
        owner = new CommandsManagerTestOwner();
        target = new CommandsManager(owner, new TaskFactory());
    }
    Expression GetBodyExpression(Expression<Func<object, bool>> expression) => expression.Body;
    [TestFixture]
    public class ExtractPropertyNames: CommandsManagerTest
    {
        [Test]
        public void WhenConstantCondition_NoPropertyNamesInResult()
        {
            var lambda = GetBodyExpression(o => true);
            var actual = target.ExtractPropertyNames(lambda);

            Assert.That(actual.Count, Is.Zero);
        }
        [Test]
        public void WhenSimpleBooleanMemberProperty_PropertyNameInResult()
        {
            var lambda = GetBodyExpression(o => owner.SimpleCondition);
            var actual = target.ExtractPropertyNames(lambda);

            Assert.That(actual, Is.EqualTo(ImmutableHashSet<string>.Empty.Add(nameof(owner.SimpleCondition))));
        }
        [Test]
        public void WhenOtherSimpleBooleanMemberProperty_PropertyNameInResult()
        {
            var lambda = GetBodyExpression(o => owner.OtherSimpleCondition);
            var actual = target.ExtractPropertyNames(lambda);

            Assert.That(actual, Is.EqualTo(ImmutableHashSet<string>.Empty.Add(nameof(owner.OtherSimpleCondition))));
        }
        bool NonMember => true;
        [Test]
        public void WhenNonMemberProperty_NoPropertyNamesInResult()
        {
            var lambda = GetBodyExpression(o => NonMember);
            var actual = target.ExtractPropertyNames(lambda);

            Assert.That(actual.Count, Is.Zero);
        }
        [Test]
        public void WhenBinaryOperationOnTwoProperties_BothNamesInResult()
        {
            var lambda = GetBodyExpression(o => owner.SimpleCondition || owner.OtherSimpleCondition);
            var actual = target.ExtractPropertyNames(lambda);

            Assert.That(actual, Is.EqualTo(ImmutableHashSet<string>.Empty.Add(nameof(owner.OtherSimpleCondition)).Add(nameof(owner.SimpleCondition))));
        }
        [Test]
        public void WhenPropertyIsMethodArgument_PropertyNameInResult()
        {
            var lambda = GetBodyExpression(o => owner.SimpleMethod(owner.SimpleCondition));
            var actual = target.ExtractPropertyNames(lambda);

            Assert.That(actual, Is.EqualTo(ImmutableHashSet<string>.Empty.Add(nameof(owner.SimpleCondition))));
        }
        [Test]
        public void WhenUsingConditionalExpression_BothNamesInResult()
        {
            var lambda = GetBodyExpression(o => owner.SimpleCondition ? owner.OtherSimpleCondition: false);
            var actual = target.ExtractPropertyNames(lambda);

            Assert.That(actual, Is.EqualTo(ImmutableHashSet<string>.Empty.Add(nameof(owner.OtherSimpleCondition)).Add(nameof(owner.SimpleCondition))));
        }
        [Test]
        public void WhenUnaryExpressionIsUsed_PropertyNameInResult()
        {
            var lambda = GetBodyExpression(o => !owner.SimpleMethod(owner.SimpleCondition));
            var actual = target.ExtractPropertyNames(lambda);

            Assert.That(actual, Is.EqualTo(ImmutableHashSet<string>.Empty.Add(nameof(owner.SimpleCondition))));
        }
    }
}

public class CommandsManagerTestOwner: NotifiableObject
{
    public bool SimpleCondition => true;
    public bool OtherSimpleCondition => false;
    public bool SimpleMethod(bool one) => one;
}
