using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

internal sealed class UlongOrderRow
{
    [Key]
    public int Id { get; set; }

    public ulong? Value { get; set; }
}

public class OrderByNullableUlongNullsOrderBugTests
{
    private static readonly (int Id, ulong? Value)[] Seed =
    [
        (1, 5UL),
        (2, ulong.MaxValue),
        (3, null),
        (4, 1UL << 63),
    ];

    private static UlongOrderRow[] CreateRows()
    {
        return Seed.Select(r => new UlongOrderRow { Id = r.Id, Value = r.Value }).ToArray();
    }

    [Fact]
    public void OrderByAscending_NullsLast_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<UlongOrderRow>().Schema.CreateTable();
        foreach (UlongOrderRow row in CreateRows())
        {
            db.Table<UlongOrderRow>().Add(row);
        }

        List<int> expected = Seed.OrderBy(r => r.Value == null ? 1 : 0).ThenBy(r => r.Value).Select(r => r.Id).ToList();
        List<int> actual = db.Table<UlongOrderRow>().OrderBy(x => x.Value, SQLiteNullsOrder.Last).Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByDescending_NullsFirst_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<UlongOrderRow>().Schema.CreateTable();
        foreach (UlongOrderRow row in CreateRows())
        {
            db.Table<UlongOrderRow>().Add(row);
        }

        List<int> expected = Seed.OrderBy(r => r.Value == null ? 0 : 1).ThenByDescending(r => r.Value).Select(r => r.Id).ToList();
        List<int> actual = db.Table<UlongOrderRow>().OrderByDescending(x => x.Value, SQLiteNullsOrder.First).Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }
}
