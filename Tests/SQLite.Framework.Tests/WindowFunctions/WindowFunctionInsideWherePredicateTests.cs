using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25pTicks")]
public class H25pTick
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public class WindowFunctionInsideWherePredicateTests
{
    [Fact]
    public void AWindowFunctionUsedDirectlyInAWherePredicateReportsAClearMessage()
    {
        using TestDatabase db = Setup(nameof(AWindowFunctionUsedDirectlyInAWherePredicateReportsAClearMessage));

        Assert.Throws<NotSupportedException>(() => db.Table<H25pTick>()
            .Where(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).AsValue() <= 2)
            .ToList());
    }

    [Fact]
    public void AWindowFunctionUsedInAGroupByKeyReportsAClearMessage()
    {
        using TestDatabase db = Setup(nameof(AWindowFunctionUsedInAGroupByKeyReportsAClearMessage));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<H25pTick>()
            .GroupBy(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).AsValue())
            .Select(g => g.Count())
            .ToList());

        Assert.Contains("GroupBy key", ex.Message);
    }

    private static List<H25pTick> Rows()
    {
        return
        [
            new H25pTick { Id = 1, Amount = 30 },
            new H25pTick { Id = 2, Amount = 10 },
            new H25pTick { Id = 3, Amount = 20 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25pTick>().Schema.CreateTable();
        db.Table<H25pTick>().AddRange(Rows());
        return db;
    }
}
