using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class UlongNullRow
{
    [Key]
    public int Id { get; set; }
    public ulong? Value { get; set; }
}

public class OrderByNullableUlongNullsTests
{
    private static readonly (int Id, ulong? Value)[] Seed =
    [
        (1, 5UL),
        (2, ulong.MaxValue),
        (3, null),
        (4, 1UL << 63),
        (5, 0UL),
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<UlongNullRow>().Schema.CreateTable();
        foreach ((int id, ulong? value) in Seed)
        {
            db.Table<UlongNullRow>().Add(new UlongNullRow { Id = id, Value = value });
        }

        return db;
    }

    [Fact]
    public void Ascending_NullsLast_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.OrderBy(r => r.Value == null ? 1 : 0).ThenBy(r => r.Value).Select(r => r.Id).ToList();
        List<int> actual = db.Table<UlongNullRow>().OrderBy(x => x.Value, SQLiteNullsOrder.Last).Select(x => x.Id).ToList();

        Assert.Equal([5, 1, 4, 2, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Ascending_NullsFirst_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.OrderBy(r => r.Value == null ? 0 : 1).ThenBy(r => r.Value).Select(r => r.Id).ToList();
        List<int> actual = db.Table<UlongNullRow>().OrderBy(x => x.Value, SQLiteNullsOrder.First).Select(x => x.Id).ToList();

        Assert.Equal([3, 5, 1, 4, 2], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Descending_NullsFirst_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.OrderBy(r => r.Value == null ? 0 : 1).ThenByDescending(r => r.Value).Select(r => r.Id).ToList();
        List<int> actual = db.Table<UlongNullRow>().OrderByDescending(x => x.Value, SQLiteNullsOrder.First).Select(x => x.Id).ToList();

        Assert.Equal([3, 2, 4, 1, 5], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Descending_NullsLast_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.OrderBy(r => r.Value == null ? 1 : 0).ThenByDescending(r => r.Value).Select(r => r.Id).ToList();
        List<int> actual = db.Table<UlongNullRow>().OrderByDescending(x => x.Value, SQLiteNullsOrder.Last).Select(x => x.Id).ToList();

        Assert.Equal([2, 4, 1, 5, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Ascending_NoNullsClause_OrdersUnsignedValuesCorrectly()
    {
        using TestDatabase db = CreateDb();

        List<int> actual = db.Table<UlongNullRow>()
            .Where(x => x.Value != null)
            .OrderBy(x => x.Value)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal([5, 1, 4, 2], actual);
    }

    [Fact]
    public void NullableInt_NullsLast_StillWorks()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        (int Id, int? Value)[] seed = [(1, 5), (2, null), (3, 1), (4, null)];
        foreach ((int id, int? value) in seed)
        {
            db.Table<NullableEntity>().Add(new NullableEntity { Id = id, Value = value });
        }

        List<int> expected = seed.OrderBy(r => r.Value == null ? 1 : 0).ThenBy(r => r.Value).Select(r => r.Id).ToList();
        List<int> actual = db.Table<NullableEntity>().OrderBy(x => x.Value, SQLiteNullsOrder.Last).Select(x => x.Id).ToList();

        Assert.Equal([3, 1, 2, 4], expected);
        Assert.Equal(expected, actual);
    }
}
