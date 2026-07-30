using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25lNullableAmountRows")]
public class H25lNullableAmountRow
{
    [Key]
    public int Id { get; set; }

    public decimal? Amount { get; set; }
}

public class NullableTextDecimalToStringFilterTests
{
    [Fact]
    public void FiltersOnTheTextLengthOfANullableTextStoredDecimal()
    {
        using TestDatabase db = Setup(nameof(FiltersOnTheTextLengthOfANullableTextStoredDecimal));
        List<H25lNullableAmountRow> local = Rows();

        List<int> expected = local
            .Where(r => r.Amount.ToString()!.Length == 5)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H25lNullableAmountRow>()
            .Where(r => r.Amount.ToString()!.Length == 5)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FiltersOnTheTrailingZeroOfANullableTextStoredDecimal()
    {
        using TestDatabase db = Setup(nameof(FiltersOnTheTrailingZeroOfANullableTextStoredDecimal));
        List<H25lNullableAmountRow> local = Rows();

        List<int> expected = local
            .Where(r => r.Amount.ToString()!.EndsWith("0"))
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H25lNullableAmountRow>()
            .Where(r => r.Amount.ToString()!.EndsWith("0"))
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReadsTheSameTextLengthInAFilterAndInAProjection()
    {
        using TestDatabase db = Setup(nameof(ReadsTheSameTextLengthInAFilterAndInAProjection));

        List<int> projected = db.Table<H25lNullableAmountRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Amount.ToString()!.Length)
            .ToList();

        int projectedLength = projected[0];

        List<int> filteredIds = db.Table<H25lNullableAmountRow>()
            .Where(r => r.Amount.ToString()!.Length == projectedLength)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(5, projectedLength);
        Assert.Single(filteredIds);
        Assert.Equal(1, filteredIds[0]);
    }

    private static List<H25lNullableAmountRow> Rows()
    {
        return
        [
            new H25lNullableAmountRow { Id = 1, Amount = 10.50m },
            new H25lNullableAmountRow { Id = 2, Amount = 7.25m },
            new H25lNullableAmountRow { Id = 3, Amount = null }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text), methodName);
        db.Table<H25lNullableAmountRow>().Schema.CreateTable();
        db.Table<H25lNullableAmountRow>().AddRange(Rows());
        return db;
    }
}
