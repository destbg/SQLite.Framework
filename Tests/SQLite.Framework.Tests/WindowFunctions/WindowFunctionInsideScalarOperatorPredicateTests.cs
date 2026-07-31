using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26dPredicateTicks")]
public class H26dPredicateTick
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public class WindowFunctionInsideScalarOperatorPredicateTests
{
    [Fact]
    public void AWindowFunctionInsideACountPredicateReportsAClearMessage()
    {
        using TestDatabase db = Setup(nameof(AWindowFunctionInsideACountPredicateReportsAClearMessage));

        Assert.Throws<NotSupportedException>(() => db.Table<H26dPredicateTick>()
            .Count(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).AsValue() <= 2));
    }

    [Fact]
    public void AWindowFunctionInsideAFirstPredicateReportsAClearMessage()
    {
        using TestDatabase db = Setup(nameof(AWindowFunctionInsideAFirstPredicateReportsAClearMessage));

        Assert.Throws<NotSupportedException>(() => db.Table<H26dPredicateTick>()
            .First(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).AsValue() <= 2));
    }

    [Fact]
    public void AWindowFunctionInsideAnAnyPredicateReportsAClearMessage()
    {
        using TestDatabase db = Setup(nameof(AWindowFunctionInsideAnAnyPredicateReportsAClearMessage));

        Assert.Throws<NotSupportedException>(() => db.Table<H26dPredicateTick>()
            .Any(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).AsValue() <= 2));
    }

    [Fact]
    public void AWindowFunctionInsideAnAllPredicateReportsAClearMessage()
    {
        using TestDatabase db = Setup(nameof(AWindowFunctionInsideAnAllPredicateReportsAClearMessage));

        Assert.Throws<NotSupportedException>(() => db.Table<H26dPredicateTick>()
            .All(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).AsValue() <= 2));
    }

    private static List<H26dPredicateTick> Rows()
    {
        return
        [
            new H26dPredicateTick { Id = 1, Amount = 30 },
            new H26dPredicateTick { Id = 2, Amount = 10 },
            new H26dPredicateTick { Id = 3, Amount = 20 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26dPredicateTick>().Schema.CreateTable();
        db.Table<H26dPredicateTick>().AddRange(Rows());
        return db;
    }
}
