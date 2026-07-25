using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NullableRelationalGroupingTests
{
    private static NullableEntity[] Seed() =>
    [
        new NullableEntity { Id = 1, Value = null },
        new NullableEntity { Id = 2, Value = 3 },
        new NullableEntity { Id = 3, Value = 7 },
        new NullableEntity { Id = 4, Value = null },
        new NullableEntity { Id = 5, Value = 9 },
    ];

    [Fact]
    public void GroupByNullableRelationalKeyCountsMatchDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        NullableEntity[] rows = Seed();
        foreach (NullableEntity r in rows)
        {
            db.Table<NullableEntity>().Add(r);
        }

        List<int> oracle = rows
            .GroupBy(x => x.Value > 5)
            .OrderBy(g => g.Key)
            .Select(g => g.Count())
            .ToList();

        List<int> actual = db.Table<NullableEntity>()
            .GroupBy(x => x.Value > 5)
            .OrderBy(g => g.Key)
            .Select(g => g.Count())
            .ToList();

        Assert.Equal([3, 2], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctNullableRelationalCardinalityMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        NullableEntity[] rows = Seed();
        foreach (NullableEntity r in rows)
        {
            db.Table<NullableEntity>().Add(r);
        }

        List<bool> oracle = rows.Select(x => x.Value > 5).Distinct().OrderBy(b => b).ToList();
        List<bool> actual = db.Table<NullableEntity>().Select(x => x.Value > 5).Distinct().OrderBy(b => b).ToList();

        Assert.Equal([false, true], oracle);
        Assert.Equal(oracle, actual);
    }
}
