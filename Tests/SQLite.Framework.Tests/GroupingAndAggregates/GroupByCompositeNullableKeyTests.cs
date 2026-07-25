using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupByCompositeNullableKeyTests
{
    private static TwoNullableIntEntity[] Seed() =>
    [
        new TwoNullableIntEntity { Id = 1, A = 1, B = 1 },
        new TwoNullableIntEntity { Id = 2, A = 1, B = 1 },
        new TwoNullableIntEntity { Id = 3, A = 1, B = 2 },
        new TwoNullableIntEntity { Id = 4, A = 2, B = 1 },
        new TwoNullableIntEntity { Id = 5, A = null, B = 1 },
        new TwoNullableIntEntity { Id = 6, A = null, B = 1 },
    ];

    [Fact]
    public void GroupByCompositeNullableValueTypeKeyMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<TwoNullableIntEntity>().Schema.CreateTable();
        TwoNullableIntEntity[] rows = Seed();
        foreach (TwoNullableIntEntity r in rows)
        {
            db.Table<TwoNullableIntEntity>().Add(r);
        }

        var oracle = rows
            .GroupBy(x => new { x.A, x.B })
            .Select(g => new { g.Key.A, g.Key.B, Cnt = g.Count() })
            .OrderBy(x => x.A).ThenBy(x => x.B)
            .ToList();

        var actual = db.Table<TwoNullableIntEntity>()
            .GroupBy(x => new { x.A, x.B })
            .Select(g => new { g.Key.A, g.Key.B, Cnt = g.Count() })
            .OrderBy(x => x.A).ThenBy(x => x.B)
            .ToList();

        Assert.Equal(4, oracle.Count);
        Assert.Equal(oracle.Count, actual.Count);
        for (int i = 0; i < oracle.Count; i++)
        {
            Assert.Equal(oracle[i].A, actual[i].A);
            Assert.Equal(oracle[i].B, actual[i].B);
            Assert.Equal(oracle[i].Cnt, actual[i].Cnt);
        }
    }
}
