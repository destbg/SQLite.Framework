using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26uPricedRows")]
public class H26uPricedRow
{
    [Key]
    public int Id { get; set; }

    public decimal Price { get; set; }
}

public class H26uRankedPrice
{
    public decimal Price { get; set; }

    public long Position { get; set; }
}

public class WindowKeyFromScalarCteElementTests
{
    [Fact]
    public void AWindowOrderedByAScalarDecimalCommonTableExpressionElementRanksNumerically()
    {
        using TestDatabase db = Setup(nameof(AWindowOrderedByAScalarDecimalCommonTableExpressionElementRanksNumerically));

        List<decimal> expected = Rows().Select(r => r.Price).OrderBy(p => p).ToList();

        SQLiteCte<decimal> cte = db.With(() => db.Table<H26uPricedRow>().Select(r => r.Price));

        List<decimal> actual = cte
            .Select(v => new H26uRankedPrice
            {
                Price = v,
                Position = SQLiteWindowFunctions.RowNumber().Over().OrderBy(v)
            })
            .AsEnumerable()
            .OrderBy(x => x.Position)
            .Select(x => x.Price)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AWindowMinimumOverAScalarDecimalCommonTableExpressionElementComparesNumerically()
    {
        using TestDatabase db = Setup(nameof(AWindowMinimumOverAScalarDecimalCommonTableExpressionElementComparesNumerically));

        decimal expected = Rows().Select(r => r.Price).Min();

        SQLiteCte<decimal> cte = db.With(() => db.Table<H26uPricedRow>().Select(r => r.Price));

        decimal actual = cte
            .Select(v => SQLiteWindowFunctions.Min(v).Over().AsValue())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AWindowMaximumOverAScalarDecimalCommonTableExpressionElementComparesNumerically()
    {
        using TestDatabase db = Setup(nameof(AWindowMaximumOverAScalarDecimalCommonTableExpressionElementComparesNumerically));

        decimal expected = Rows().Select(r => r.Price).Max();

        SQLiteCte<decimal> cte = db.With(() => db.Table<H26uPricedRow>().Select(r => r.Price));

        decimal actual = cte
            .Select(v => SQLiteWindowFunctions.Max(v).Over().AsValue())
            .First();

        Assert.Equal(expected, actual);
    }

    private static List<H26uPricedRow> Rows()
    {
        return
        [
            new H26uPricedRow { Id = 1, Price = 2.75m },
            new H26uPricedRow { Id = 2, Price = 9.5m },
            new H26uPricedRow { Id = 3, Price = 150m }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text), methodName);
        db.Table<H26uPricedRow>().Schema.CreateTable();
        db.Table<H26uPricedRow>().AddRange(Rows());
        return db;
    }
}
