using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("NullableDecimalTextRows")]
public class NullableDecimalTextRow
{
    [Key]
    public int Id { get; set; }

    public decimal? Amount { get; set; }
}

public class NullableDecimalTextOrderingTests
{
    [Fact]
    public void OrderByANullableDecimalColumnSortsByValue()
    {
        using TestDatabase db = Setup(nameof(OrderByANullableDecimalColumnSortsByValue));

        List<decimal?> expected = Rows().OrderBy(r => r.Amount).Select(r => r.Amount).ToList();
        List<decimal?> actual = db.Table<NullableDecimalTextRow>()
            .OrderBy(r => r.Amount)
            .Select(r => r.Amount)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByANullableDecimalColumnAfterUnionSortsByValue()
    {
        using TestDatabase db = Setup(nameof(OrderByANullableDecimalColumnAfterUnionSortsByValue));

        List<decimal?> expected = Rows().Where(r => r.Id != 3)
            .Union(Rows().Where(r => r.Id == 3))
            .OrderBy(r => r.Amount)
            .Select(r => r.Amount)
            .ToList();

        List<decimal?> actual = db.Table<NullableDecimalTextRow>().Where(r => r.Id != 3)
            .Union(db.Table<NullableDecimalTextRow>().Where(r => r.Id == 3))
            .OrderBy(r => r.Amount)
            .AsEnumerable()
            .Select(r => r.Amount)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<NullableDecimalTextRow> Rows()
    {
        return
        [
            new NullableDecimalTextRow { Id = 1, Amount = 10.11m },
            new NullableDecimalTextRow { Id = 2, Amount = 9.99m },
            new NullableDecimalTextRow { Id = 3, Amount = null }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text), methodName);
        db.Table<NullableDecimalTextRow>().Schema.CreateTable();
        db.Table<NullableDecimalTextRow>().AddRange(Rows());
        return db;
    }
}
