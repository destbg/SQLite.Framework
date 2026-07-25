using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ComputedNullableEqualitySemanticsTests
{
    private static readonly (int Id, int? A, int? B)[] Rows =
    [
        (1, null, null),
        (2, 1, 1),
        (3, 1, null),
    ];

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

    [Fact]
    public void Equality_BetweenComputedNullableValues_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<int> oracle = Rows
            .Where(e => e.A + e.B == e.B + e.A)
            .Select(e => e.Id)
            .ToList();

        List<int> actual = db.Table<TwoNullableIntEntity>()
            .Where(e => e.A + e.B == e.B + e.A)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([1, 2, 3], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Inequality_BetweenComputedNullableValues_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<int> oracle = Rows
            .Where(e => e.A + e.B != e.B + e.A)
            .Select(e => e.Id)
            .ToList();

        List<int> actual = db.Table<TwoNullableIntEntity>()
            .Where(e => e.A + e.B != e.B + e.A)
            .Select(e => e.Id)
            .ToList();

        Assert.Empty(oracle);
        Assert.Equal(oracle, actual);
    }
}
