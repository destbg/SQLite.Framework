#if NET11_0_OR_GREATER
using System.Reflection;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class Net11SpanAggregateTests
{
    [Fact]
    public void ArrayMinAndMaxWithNullComparerTranslateInsidePredicates()
    {
        using TestDatabase db = new();
        db.Table<Net11JoinRow>().Schema.CreateTable();
        db.Table<Net11JoinRow>().AddRange([new() { Id = 1 }, new() { Id = 8 }]);
        Assert.Equal([1], db.Table<Net11JoinRow>().Where(x => new[] { x.Id, 6 }.Min(null) < 5).Select(x => x.Id).ToList());
        Assert.Equal([8], db.Table<Net11JoinRow>().Where(x => new[] { x.Id, 6 }.Max(null) > 6).Select(x => x.Id).ToList());
    }

    [Fact]
    public void ArrayMinRejectsCustomComparer()
    {
        using TestDatabase db = new();
        Assert.Throws<NotSupportedException>(() => db.Table<Net11JoinRow>()
            .Where(x => new[] { x.Id, 6 }.Min(Comparer<int>.Default) < 5).ToList());
    }

    [Fact]
    public void ArrayMinAndMaxHonorRegisteredMethodTranslators()
    {
        using TestDatabase db = new(builder =>
        {
            foreach (MethodInfo method in typeof(MemoryExtensions).GetMethods()
                .Where(m => m.Name is nameof(MemoryExtensions.Min) or nameof(MemoryExtensions.Max)))
            {
                builder.AddMethodTranslator(method.MakeGenericMethod(typeof(int)),
                    ctx => SQLiteExpression.Leaf(typeof(int), ctx.Counters.NextIdentifier(), "42"));
            }
        });
        db.Table<Net11JoinRow>().Schema.CreateTable();
        db.Table<Net11JoinRow>().Add(new() { Id = 1 });
        Assert.Equal(1, db.Table<Net11JoinRow>().Count(x => new[] { x.Id, 6 }.Min() == 42));
        Assert.Equal(1, db.Table<Net11JoinRow>().Count(x => new[] { x.Id, 6 }.Min(null) == 42));
        Assert.Equal(1, db.Table<Net11JoinRow>().Count(x => new[] { x.Id, 6 }.Max() == 42));
        Assert.Equal(1, db.Table<Net11JoinRow>().Count(x => new[] { x.Id, 6 }.Max(null) == 42));
    }
}
#endif
