using System.Linq.Expressions;
using SQLite.Framework.Internals.JSON;

namespace SQLite.Framework.Tests;

public static class JsonGroupAggregateFinderHelper
{
    public static readonly List<int> Values = [1, 2, 3];

    public static int Tally(IGrouping<int, int> grouping)
    {
        return grouping.Count();
    }
}

public class JsonGroupAggregateFinderTests
{
    [Fact]
    public void FindsAnAggregateOverTheGroupingParameter()
    {
        Assert.True(Probe(g => g.Count()));
    }

    [Fact]
    public void StopsSearchingAfterTheFirstAggregate()
    {
        Assert.True(Probe(g => g.Count() + g.Sum()));
    }

    [Fact]
    public void IgnoresContainsOverTheGroupingParameter()
    {
        Assert.False(Probe(g => g.Contains(5)));
    }

    [Fact]
    public void IgnoresAggregatesOverAnotherSource()
    {
        Assert.False(Probe(g => JsonGroupAggregateFinderHelper.Values.Count(v => v > 0)));
    }

    [Fact]
    public void IgnoresEnumerableCallsWithoutArguments()
    {
        Assert.False(Probe(g => Enumerable.Empty<int>().ToList().Count));
    }

    [Fact]
    public void IgnoresCallsDeclaredOutsideEnumerable()
    {
        Assert.False(Probe(g => JsonGroupAggregateFinderHelper.Tally(g)));
    }

    private static bool Probe<TResult>(Expression<Func<IGrouping<int, int>, TResult>> lambda)
    {
        JsonGroupAggregateFinder finder = new(lambda.Parameters[0]);
        finder.Visit(lambda.Body);
        return finder.Found;
    }
}
