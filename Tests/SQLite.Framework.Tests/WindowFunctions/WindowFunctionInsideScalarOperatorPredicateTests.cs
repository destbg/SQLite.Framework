using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
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
    public void CountPredicateUsesIndependentRowNumbers()
    {
        using TestDatabase db = Setup(nameof(CountPredicateUsesIndependentRowNumbers));
        Dictionary<int, long> rowNumbers = RowNumbers(Rows());
        int expected = Rows().Count(r => rowNumbers[r.Id] <= 4 && r.Amount >= 0);

        int actual = db.Table<H26dPredicateTick>()
            .Count(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 4
                && r.Amount >= 0);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstPredicateKeepsTheSourceOrder()
    {
        using TestDatabase db = Setup(nameof(FirstPredicateKeepsTheSourceOrder));
        List<H26dPredicateTick> rows = Rows();
        Dictionary<int, long> rowNumbers = RowNumbers(rows);
        int expected = rows.OrderBy(r => r.Id).First(r => rowNumbers[r.Id] <= 4 && r.Amount >= 0).Id;

        int actual = db.Table<H26dPredicateTick>()
            .OrderBy(r => r.Id)
            .First(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 4
                && r.Amount >= 0)
            .Id;

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AnyPredicateHonorsRankBoundaries(int maximumRank)
    {
        using TestDatabase db = Setup(nameof(AnyPredicateHonorsRankBoundaries));
        Dictionary<int, long> rowNumbers = RowNumbers(Rows());
        bool expected = Rows().Any(r => rowNumbers[r.Id] <= maximumRank);

        bool actual = db.Table<H26dPredicateTick>()
            .Any(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= maximumRank);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void AllPredicateHonorsRankBoundaries(int maximumRank)
    {
        using TestDatabase db = Setup(nameof(AllPredicateHonorsRankBoundaries));
        Dictionary<int, long> rowNumbers = RowNumbers(Rows());
        bool expected = Rows().All(r => rowNumbers[r.Id] <= maximumRank);

        bool actual = db.Table<H26dPredicateTick>()
            .All(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= maximumRank);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EmptySourceKeepsPredicateTerminalSemantics()
    {
        using TestDatabase db = Setup(nameof(EmptySourceKeepsPredicateTerminalSemantics), addRows: false);
        List<H26dPredicateTick> rows = [];
        Dictionary<int, long> rowNumbers = RowNumbers(rows);
        int expectedCount = rows.Count(r => rowNumbers[r.Id] <= 2);
        bool expectedAny = rows.Any(r => rowNumbers[r.Id] <= 2);
        bool expectedAll = rows.All(r => rowNumbers[r.Id] <= 2);

        int count = db.Table<H26dPredicateTick>()
            .Count(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 2);
        bool any = db.Table<H26dPredicateTick>()
            .Any(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 2);
        bool all = db.Table<H26dPredicateTick>()
            .All(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 2);

        Assert.Equal(expectedCount, count);
        Assert.Equal(expectedAny, any);
        Assert.Equal(expectedAll, all);
        Assert.Throws<InvalidOperationException>(() => rows.First(r => rowNumbers[r.Id] <= 2));
        Assert.Throws<InvalidOperationException>(() => db.Table<H26dPredicateTick>()
            .First(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 2));
    }

    [Fact]
    public void OtherPredicateTerminalsKeepTheirNormalResultRules()
    {
        using TestDatabase db = Setup(nameof(OtherPredicateTerminalsKeepTheirNormalResultRules));
        List<H26dPredicateTick> rows = Rows();
        Dictionary<int, long> rowNumbers = RowNumbers(rows);
        long expectedCount = rows.LongCount(r => rowNumbers[r.Id] <= 2);
        int expectedSingleId = rows.Single(r => rowNumbers[r.Id] <= 1).Id;
        H26dPredicateTick fallback = new() { Id = 99, Amount = -99 };
        H26dPredicateTick expectedFallback = rows.FirstOrDefault(r => rowNumbers[r.Id] <= 0, fallback);
        H26dPredicateTick? expectedDefault = rows.SingleOrDefault(r => rowNumbers[r.Id] <= 0);

        long actualCount = db.Table<H26dPredicateTick>()
            .LongCount(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 2);
        int actualSingleId = db.Table<H26dPredicateTick>()
            .Single(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 1)
            .Id;
        H26dPredicateTick actualFallback = db.Table<H26dPredicateTick>()
            .FirstOrDefault(
                r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 0,
                fallback);
        H26dPredicateTick? actualDefault = db.Table<H26dPredicateTick>()
            .SingleOrDefault(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 0);

        Assert.Equal(expectedCount, actualCount);
        Assert.Equal(expectedSingleId, actualSingleId);
        Assert.Equal(expectedFallback.Id, actualFallback.Id);
        Assert.Equal(expectedDefault?.Id, actualDefault?.Id);
        Assert.Throws<InvalidOperationException>(() => rows.Single(r => rowNumbers[r.Id] <= 2));
        Assert.Throws<InvalidOperationException>(() => db.Table<H26dPredicateTick>()
            .Single(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 2));
    }

    [Fact]
    public void OrderedDirectWindowPredicateKeepsTheOuterOrderInSql()
    {
        using TestDatabase db = Setup(nameof(OrderedDirectWindowPredicateKeepsTheOuterOrderInSql), addRows: false);

        SQLiteCommand command = db.Table<H26dPredicateTick>()
            .OrderBy(r => r.Id)
            .Where(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 4
                && r.Amount >= 0)
            .Take(1)
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT h1."Id" AS "Id",
                            h1."Amount" AS "Amount"
                     FROM (
                         SELECT h0."Id" AS "Id",
                            h0."Amount" AS "Amount",
                            ROW_NUMBER() OVER ( ORDER BY h0."Amount" ASC, h0."Id" ASC) <= @p0 AND h0."Amount" >= @p1 AS "__WindowValue"
                         FROM "H26dPredicateTicks" AS h0
                     ) AS h1
                     WHERE h1."__WindowValue"
                     ORDER BY h1."Id" ASC
                     LIMIT 1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void DescendingAndSecondarySourceOrdersStayOutsideTheWindowProjection()
    {
        using TestDatabase db = Setup(nameof(DescendingAndSecondarySourceOrdersStayOutsideTheWindowProjection));
        List<H26dPredicateTick> rows = Rows();
        Dictionary<int, long> rowNumbers = RowNumbers(rows);

        int expectedDescending = rows.OrderByDescending(r => r.Amount)
            .ThenBy(r => r.Id)
            .First(r => rowNumbers[r.Id] <= 4)
            .Id;
        int actualDescending = db.Table<H26dPredicateTick>()
            .OrderByDescending(r => r.Amount)
            .ThenBy(r => r.Id)
            .First(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 4)
            .Id;
        int expectedSecondary = rows.OrderBy(r => r.Amount)
            .ThenByDescending(r => r.Id)
            .First(r => rowNumbers[r.Id] <= 4)
            .Id;
        int actualSecondary = db.Table<H26dPredicateTick>()
            .OrderBy(r => r.Amount)
            .ThenByDescending(r => r.Id)
            .First(r => SQLiteWindowFunctions.RowNumber().OrderBy(r.Amount).ThenOrderBy(r.Id).AsValue() <= 4)
            .Id;

        Assert.Equal(expectedDescending, actualDescending);
        Assert.Equal(expectedSecondary, actualSecondary);
    }

    [Fact]
    public void NaturalScalarSourceOrdersStayOutsideTheWindowProjection()
    {
        using TestDatabase db = Setup(nameof(NaturalScalarSourceOrdersStayOutsideTheWindowProjection));
        int[] ids = Rows().Select(r => r.Id).ToArray();

        int expectedAscending = ids.Order().First(id => id <= 2);
        int actualAscending = db.Table<H26dPredicateTick>()
            .Select(r => r.Id)
            .Order()
            .First(id => SQLiteWindowFunctions.RowNumber().OrderBy(id).AsValue() <= 2);
        int expectedDescending = ids.OrderDescending().First(id => id <= 2);
        int actualDescending = db.Table<H26dPredicateTick>()
            .Select(r => r.Id)
            .OrderDescending()
            .First(id => SQLiteWindowFunctions.RowNumber().OrderBy(id).AsValue() <= 2);

        Assert.Equal(expectedAscending, actualAscending);
        Assert.Equal(expectedDescending, actualDescending);
    }

    private static List<H26dPredicateTick> Rows()
    {
        return
        [
            new H26dPredicateTick { Id = 1, Amount = 20 },
            new H26dPredicateTick { Id = 2, Amount = -5 },
            new H26dPredicateTick { Id = 3, Amount = 20 },
            new H26dPredicateTick { Id = 4, Amount = 0 },
            new H26dPredicateTick { Id = 5, Amount = 30 },
            new H26dPredicateTick { Id = 6, Amount = -5 }
        ];
    }

    private static Dictionary<int, long> RowNumbers(IEnumerable<H26dPredicateTick> rows)
    {
        return rows
            .OrderBy(r => r.Amount)
            .ThenBy(r => r.Id)
            .Select((r, index) => (r.Id, RowNumber: (long)index + 1))
            .ToDictionary(r => r.Id, r => r.RowNumber);
    }

    private static TestDatabase Setup(string methodName, bool addRows = true)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26dPredicateTick>().Schema.CreateTable();
        if (addRows)
        {
            db.Table<H26dPredicateTick>().AddRange(Rows());
        }
        return db;
    }
}
