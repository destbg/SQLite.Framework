using System.Linq.Expressions;
using SQLite.Framework.Internals.Visitors;

namespace SQLite.Framework.Tests;

public class ParameterSubstitutorInternalTests
{
    [Fact]
    public void Visit_ParameterMatchesTarget_ReturnsReplacement()
    {
        ParameterExpression target = Expression.Parameter(typeof(int), "x");
        Expression replacement = Expression.Constant(42);
        ParameterSubstitutor sut = new(target, replacement);

        Expression result = sut.Visit(target);

        Assert.Same(replacement, result);
    }

    [Fact]
    public void Visit_ParameterDoesNotMatchTarget_ReturnsOriginal()
    {
        ParameterExpression target = Expression.Parameter(typeof(int), "x");
        ParameterExpression other = Expression.Parameter(typeof(int), "y");
        ParameterSubstitutor sut = new(target, Expression.Constant(99));

        Expression result = sut.Visit(other);

        Assert.Same(other, result);
    }
}
