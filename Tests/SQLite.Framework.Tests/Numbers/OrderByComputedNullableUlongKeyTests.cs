using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ComputedUlongRow
{
    [Key]
    public int Id { get; set; }
    public ulong? Value { get; set; }
}

public class OrderByComputedNullableUlongKeyTests
{
    private static readonly (int Id, ulong? Value)[] Seed =
    [
        (1, 9223372036854775808UL),
        (2, 3UL),
        (3, 5UL),
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<ComputedUlongRow>().Schema.CreateTable();
        foreach ((int id, ulong? value) in Seed)
        {
            db.Table<ComputedUlongRow>().Add(new ComputedUlongRow { Id = id, Value = value });
        }

        return db;
    }

    [Fact]
    public void OrderByIncrementedNullableUlongUsesUnsignedOrder()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed
            .OrderBy(r => r.Value + 1)
            .Select(r => r.Id)
            .ToList();
        Assert.Equal([2, 3, 1], expected);

        List<int> actual = db.Table<ComputedUlongRow>()
            .OrderBy(x => x.Value + 1)
            .Select(x => x.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByIncrementedPlainUlongControl()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed
            .OrderBy(r => r.Value!.Value + 1)
            .Select(r => r.Id)
            .ToList();
        Assert.Equal([2, 3, 1], expected);

        List<int> actual = db.Table<ComputedUlongRow>()
            .OrderBy(x => x.Value!.Value + 1)
            .Select(x => x.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }
}
