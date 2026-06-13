using System.Linq.Expressions;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Tests.Entities;

namespace SQLite.Framework.Tests;

public class ParameterUsageFinderInternalTests
{
    [Fact]
    public void UsesIsTrueWhenRowIsReadAlongsideNestedLambdaParameter()
    {
        Expression<Func<Book, bool>> expr = b => new[] { 1, 2 }.Any(y => y == b.Id);

        Assert.True(ParameterUsageFinder.Uses(expr));
    }

    [Fact]
    public void UsesIsFalseWhenOnlyNestedLambdaParameterIsRead()
    {
        Expression<Func<Book, bool>> expr = b => new[] { 1, 2 }.Any(y => y == 1);

        Assert.False(ParameterUsageFinder.Uses(expr));
    }
}
