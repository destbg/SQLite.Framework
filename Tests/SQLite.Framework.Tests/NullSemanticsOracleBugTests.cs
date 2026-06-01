using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NullSemanticsOracleBugTests
{
    private static readonly (int id, int? a, int? b)[] Rows =
    {
        (1, null, null),
        (2, 5, 5),
        (3, 5, 7),
        (4, null, 5),
    };

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<TwoNullableIntEntity>().Schema.CreateTable();
        foreach ((int id, int? a, int? b) in Rows)
        {
            db.Table<TwoNullableIntEntity>().Add(new TwoNullableIntEntity { Id = id, A = a, B = b });
        }

        return db;
    }

    private static List<TwoNullableIntEntity> InMemory()
    {
        return Rows.Select(r => new TwoNullableIntEntity { Id = r.id, A = r.a, B = r.b }).ToList();
    }

    [Fact]
    public void EqualityBetweenTwoNullableColumnsMatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<int> actual = db.Table<TwoNullableIntEntity>().Where(x => x.A == x.B).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> oracle = InMemory().Where(x => x.A == x.B).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NotGreaterThanOverNullableColumnMatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<int> actual = db.Table<TwoNullableIntEntity>().Where(x => !(x.A > 3)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> oracle = InMemory().Where(x => !(x.A > 3)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NotComparisonsOverNullableColumnMatchDotNet()
    {
        using TestDatabase db = Seed();
        List<TwoNullableIntEntity> mem = InMemory();

        Assert.Equal(
            mem.Where(x => !(x.A < 3)).Select(x => x.Id).OrderBy(i => i).ToList(),
            db.Table<TwoNullableIntEntity>().Where(x => !(x.A < 3)).Select(x => x.Id).OrderBy(i => i).ToList());
        Assert.Equal(
            mem.Where(x => !(x.A >= 3)).Select(x => x.Id).OrderBy(i => i).ToList(),
            db.Table<TwoNullableIntEntity>().Where(x => !(x.A >= 3)).Select(x => x.Id).OrderBy(i => i).ToList());
        Assert.Equal(
            mem.Where(x => !(x.A <= 3)).Select(x => x.Id).OrderBy(i => i).ToList(),
            db.Table<TwoNullableIntEntity>().Where(x => !(x.A <= 3)).Select(x => x.Id).OrderBy(i => i).ToList());
        Assert.Equal(
            mem.Where(x => !(3 > x.A)).Select(x => x.Id).OrderBy(i => i).ToList(),
            db.Table<TwoNullableIntEntity>().Where(x => !(3 > x.A)).Select(x => x.Id).OrderBy(i => i).ToList());
    }
}
