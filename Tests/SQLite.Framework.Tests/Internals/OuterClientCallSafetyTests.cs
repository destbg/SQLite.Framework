using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Internals;

namespace SQLite.Framework.Tests;

public class OuterClientCallSafetyTests
{
    [Fact]
    public void SafeTailCallsWithoutALambdaAreAccepted()
    {
        Assert.True(Probe(Call(s => s.Distinct())));
        Assert.True(Probe(Call(s => s.Reverse())));
        Assert.True(Probe(Call(s => s.Take(1))));
        Assert.True(Probe(Call(s => s.Skip(1))));
        Assert.True(Probe(Call(s => s.ElementAt(0))));
        Assert.True(Probe(Call(s => s.ElementAtOrDefault(0))));
        Assert.True(Probe(Call(s => s.First())));
        Assert.True(Probe(Call(s => s.FirstOrDefault())));
        Assert.True(Probe(Call(s => s.Single())));
        Assert.True(Probe(Call(s => s.SingleOrDefault())));
        Assert.True(Probe(Call(s => s.Count())));
        Assert.True(Probe(Call(s => s.LongCount())));
        Assert.True(Probe(Call(s => s.Any())));
    }

    [Fact]
    public void SafeTailCallsWithALambdaAreRejected()
    {
        Assert.False(Probe(Call(s => s.First(v => v > 0))));
        Assert.False(Probe(Call(s => s.FirstOrDefault(v => v > 0))));
        Assert.False(Probe(Call(s => s.Single(v => v > 0))));
        Assert.False(Probe(Call(s => s.SingleOrDefault(v => v > 0))));
        Assert.False(Probe(Call(s => s.Count(v => v > 0))));
        Assert.False(Probe(Call(s => s.LongCount(v => v > 0))));
        Assert.False(Probe(Call(s => s.Any(v => v > 0))));
    }

    [Fact]
    public void CallsOutsideTheSafeSetAreRejected()
    {
        Assert.False(Probe(Call(s => s.Where(v => v > 0))));
        Assert.False(Probe(Call(s => s.Select(v => v + 1))));
        Assert.False(Probe(Call(s => s.OrderBy(v => v))));
        Assert.False(Probe(Call(s => s.OrderByDescending(v => v))));
        Assert.False(Probe(Call(s => s.GroupBy(v => v))));
        Assert.False(Probe(Call(s => s.Last())));
        Assert.False(Probe(Call(s => s.LastOrDefault())));
        Assert.False(Probe(Call(s => s.Max())));
        Assert.False(Probe(Call(s => s.Min())));
        Assert.False(Probe(Call(s => s.Sum())));
        Assert.False(Probe(Call(s => s.Average())));
        Assert.False(Probe(Call(s => s.All(v => v > 0))));
        Assert.False(Probe(Call(s => s.TakeWhile(v => v > 0))));
        Assert.False(Probe(Call(s => s.SkipWhile(v => v > 0))));
        Assert.False(Probe(Call(s => s.SkipLast(1))));
        Assert.False(Probe(Call(s => s.TakeLast(1))));
        Assert.False(Probe(Call(s => s.DefaultIfEmpty())));
        Assert.False(Probe(Call(s => s.Union(s))));
        Assert.False(Probe(Call(s => s.Concat(s))));
        Assert.False(Probe(Call(s => s.Except(s))));
        Assert.False(Probe(Call(s => s.Intersect(s))));
        Assert.False(Probe(Call(s => s.Contains(1))));
    }

    private static MethodCallExpression Call<TResult>(Expression<Func<IQueryable<int>, TResult>> query)
    {
        return (MethodCallExpression)query.Body;
    }

    private static bool Probe(MethodCallExpression call)
    {
        MethodInfo method = typeof(SQLTranslator).GetMethod(
            "OuterCallsRunOnClientValues", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (bool)method.Invoke(null, [new List<MethodCallExpression> { call }, 0])!;
    }
}
