using System.Linq.Expressions;
using SQLite.Framework.Internals.Visitors;

namespace SQLite.Framework.Tests;

public class EsfPart
{
    public int Num { get; set; }

    public string? Label { get; set; }
}

public class EsfNamedCtor
{
    public EsfNamedCtor(int num)
    {
        Num = num;
    }

    public int Num { get; set; }

    public string? Label { get; set; }
}

public class ExpressionShapeFinderTests
{
    private static bool Find(Expression body)
    {
        ConstructedConditionalFinderVisitor finder = new();
        finder.Visit(body);
        return finder.Found;
    }

    private static readonly Expression NullPart = Expression.Constant(null, typeof(EsfPart));

    private static readonly Expression NewPart = Expression.New(typeof(EsfPart));

    private static readonly Expression InitPart = Expression.MemberInit(
        Expression.New(typeof(EsfPart)),
        Expression.Bind(typeof(EsfPart).GetProperty(nameof(EsfPart.Num))!, Expression.Constant(1)));

    private static readonly Expression CapturedPart = Expression.Constant(new EsfPart(), typeof(EsfPart));

    [Fact]
    public void ConditionalShapesReportConstructedNullPairs()
    {
        Expression test = Expression.Constant(true);

        Assert.True(Find(Expression.Condition(test, NewPart, NullPart)));
        Assert.True(Find(Expression.Condition(test, InitPart, NullPart)));
        Assert.True(Find(Expression.Condition(test, NullPart, NewPart)));
        Assert.True(Find(Expression.Condition(test, NullPart, InitPart)));
        Assert.False(Find(Expression.Condition(test, NewPart, CapturedPart)));
        Assert.False(Find(Expression.Condition(test, CapturedPart, InitPart)));
        Assert.False(Find(Expression.Condition(test, CapturedPart, NullPart)));
        Assert.False(Find(Expression.Condition(test, NullPart, CapturedPart)));
    }

    [Fact]
    public void NullComparisonShapesReportConstructedOperands()
    {
        Assert.True(Find(Expression.Equal(NewPart, NullPart)));
        Assert.True(Find(Expression.Equal(InitPart, NullPart)));
        Assert.True(Find(Expression.NotEqual(NullPart, NewPart)));
        Assert.True(Find(Expression.Equal(Expression.Property(InitPart, nameof(EsfPart.Label)), Expression.Constant(null, typeof(string)))));
        Assert.True(Find(Expression.Equal(Expression.Property(Expression.Condition(Expression.Constant(true), InitPart, InitPart), nameof(EsfPart.Label)), Expression.Constant(null, typeof(string)))));
        Assert.False(Find(Expression.Equal(CapturedPart, NullPart)));
        Assert.False(Find(Expression.Equal(NullPart, CapturedPart)));
        Assert.False(Find(Expression.Equal(Expression.Constant(1), Expression.Constant(2))));
        Assert.False(Find(Expression.GreaterThan(Expression.Constant(1), Expression.Constant(2))));
    }

    [Fact]
    public void MemberReadsReportCallsInsideTheBoundValue()
    {
        Expression callValue = Expression.Call(typeof(CmcClientFns).GetMethod(nameof(CmcClientFns.Tag))!, Expression.Constant("v"));
        Expression initWithCall = Expression.MemberInit(
            Expression.New(typeof(EsfPart)),
            Expression.Bind(typeof(EsfPart).GetProperty(nameof(EsfPart.Label))!, callValue));

        Assert.True(Find(Expression.Property(initWithCall, nameof(EsfPart.Label))));
        Assert.False(Find(Expression.Property(initWithCall, nameof(EsfPart.Num))));
        Assert.False(Find(Expression.Property(InitPart, nameof(EsfPart.Num))));
        Assert.False(Find(Expression.Property(InitPart, nameof(EsfPart.Label))));
        Assert.False(Find(Expression.Property(CapturedPart, nameof(EsfPart.Num))));

        Expression invokeValue = Expression.Invoke(Expression.Constant((Func<string>)(() => "v")));
        Expression initWithInvoke = Expression.MemberInit(
            Expression.New(typeof(EsfPart)),
            Expression.Bind(typeof(EsfPart).GetProperty(nameof(EsfPart.Label))!, invokeValue));

        Assert.True(Find(Expression.Property(initWithInvoke, nameof(EsfPart.Label))));
    }

    [Fact]
    public void AnonymousMemberReadsResolveByPosition()
    {
        Expression callValue = Expression.Call(typeof(CmcClientFns).GetMethod(nameof(CmcClientFns.Tag))!, Expression.Constant("v"));
        Type anonType = new { A = 1, B = "x" }.GetType();
        NewExpression anonymous = Expression.New(
            anonType.GetConstructors()[0],
            [Expression.Constant(2), callValue],
            [anonType.GetProperty("A")!, anonType.GetProperty("B")!]);

        Assert.True(Find(Expression.Property(anonymous, "B")));
        Assert.False(Find(Expression.Property(anonymous, "A")));
    }

    [Fact]
    public void MemberReadOutsideTheConstructionMembersIsIgnored()
    {
        NewExpression partial = Expression.New(
            typeof(EsfNamedCtor).GetConstructor([typeof(int)])!,
            [Expression.Constant(3)],
            [typeof(EsfNamedCtor).GetProperty(nameof(EsfNamedCtor.Num))!]);

        Assert.False(Find(Expression.Property(partial, nameof(EsfNamedCtor.Label))));
        Assert.False(Find(Expression.Property(partial, nameof(EsfNamedCtor.Num))));
    }
}
