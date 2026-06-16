using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class NullableUlongComparisonRow
{
    [Key]
    public int Id { get; set; }

    public ulong? A { get; set; }

    public ulong? B { get; set; }
}

public class NullableUlongComparisonHighBitTests
{
    private const ulong HighBit = 9223372036854775808UL;

    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<NullableUlongComparisonRow>().Schema.CreateTable();
        db.Table<NullableUlongComparisonRow>().Add(new NullableUlongComparisonRow { Id = 1, A = null, B = HighBit });
        db.Table<NullableUlongComparisonRow>().Add(new NullableUlongComparisonRow { Id = 2, A = HighBit, B = HighBit + 1 });
        db.Table<NullableUlongComparisonRow>().Add(new NullableUlongComparisonRow { Id = 3, A = 5, B = null });
        return db;
    }

    [Fact]
    public void GreaterThanOrEqualWithNullOperandDropsRow()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = db.Table<NullableUlongComparisonRow>().AsEnumerable()
            .Where(r => r.B >= r.A)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([2], expected);

        List<int> actual = db.Table<NullableUlongComparisonRow>()
            .Where(r => r.B >= r.A)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LessThanWithNullOperandDropsRow()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = db.Table<NullableUlongComparisonRow>().AsEnumerable()
            .Where(r => r.A < r.B)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([2], expected);

        List<int> actual = db.Table<NullableUlongComparisonRow>()
            .Where(r => r.A < r.B)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
