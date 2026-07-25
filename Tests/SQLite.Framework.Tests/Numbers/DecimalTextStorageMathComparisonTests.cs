using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class DecimalTextMathRow
{
    [Key]
    public int Id { get; set; }

    public decimal Amount { get; set; }
}

public class DecimalTextStorageMathComparisonTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<DecimalTextMathRow>().Schema.CreateTable();
        db.Table<DecimalTextMathRow>().Add(new DecimalTextMathRow { Id = 1, Amount = -3.5m });
        db.Table<DecimalTextMathRow>().Add(new DecimalTextMathRow { Id = 2, Amount = 2.345m });
        db.Table<DecimalTextMathRow>().Add(new DecimalTextMathRow { Id = 3, Amount = 0m });
        db.Table<DecimalTextMathRow>().Add(new DecimalTextMathRow { Id = 4, Amount = 1.5m });
        return db;
    }

    [Fact]
    public void BareColumnComparedToDecimalConstantFiltersByValue()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = db.Table<DecimalTextMathRow>().AsEnumerable()
            .Where(r => r.Amount > 1m)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([2, 4], expected);

        List<int> actual = db.Table<DecimalTextMathRow>()
            .Where(r => r.Amount > 1m)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AbsComparedToDecimalConstantFiltersByValue()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = db.Table<DecimalTextMathRow>().AsEnumerable()
            .Where(r => Math.Abs(r.Amount) > 1m)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1, 2, 4], expected);

        List<int> actual = db.Table<DecimalTextMathRow>()
            .Where(r => Math.Abs(r.Amount) > 1m)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
