using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24iUnsignedSetRows")]
public class H24iUnsignedSetRow
{
    [Key]
    public int Id { get; set; }

    public ulong Value { get; set; }

    public ulong? Optional { get; set; }
}

public class UnsignedOrderByAfterSetOperationTests
{
    [Fact]
    public void OrderByUnsignedColumnAfterUnionMatchesUnsignedOrder()
    {
        using TestDatabase db = Setup();

        List<ulong> expected = Rows().Where(r => r.Id <= 2).Select(r => r.Value)
            .Union(Rows().Where(r => r.Id >= 3).Select(r => r.Value))
            .OrderBy(v => v)
            .ToList();

        List<ulong> actual = db.Table<H24iUnsignedSetRow>().Where(r => r.Id <= 2).Select(r => r.Value)
            .Union(db.Table<H24iUnsignedSetRow>().Where(r => r.Id >= 3).Select(r => r.Value))
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByUnsignedColumnAfterConcatOfWholeRowsMatchesUnsignedOrder()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().Where(r => r.Id <= 2)
            .Concat(Rows().Where(r => r.Id >= 3))
            .OrderBy(r => r.Value)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H24iUnsignedSetRow>().Where(r => r.Id <= 2)
            .Concat(db.Table<H24iUnsignedSetRow>().Where(r => r.Id >= 3))
            .OrderBy(r => r.Value)
            .ToList()
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByNullableUnsignedColumnAfterConcatMatchesUnsignedOrder()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().Where(r => r.Id <= 2)
            .Concat(Rows().Where(r => r.Id >= 3))
            .OrderBy(r => r.Optional)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H24iUnsignedSetRow>().Where(r => r.Id <= 2)
            .Concat(db.Table<H24iUnsignedSetRow>().Where(r => r.Id >= 3))
            .OrderBy(r => r.Optional)
            .ToList()
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H24iUnsignedSetRow>().Schema.CreateTable();
        db.Table<H24iUnsignedSetRow>().AddRange(Rows());
        return db;
    }

    private static List<H24iUnsignedSetRow> Rows()
    {
        return
        [
            new H24iUnsignedSetRow { Id = 1, Value = 1UL, Optional = null },
            new H24iUnsignedSetRow { Id = 2, Value = 2UL, Optional = 5UL },
            new H24iUnsignedSetRow { Id = 3, Value = 9223372036854775808UL, Optional = 9223372036854775808UL },
            new H24iUnsignedSetRow { Id = 4, Value = 18446744073709551615UL, Optional = 7UL }
        ];
    }
}
