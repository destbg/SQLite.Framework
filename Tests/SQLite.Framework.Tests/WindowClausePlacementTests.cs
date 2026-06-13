using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

#if !SQLITE_FRAMEWORK_BUNDLED && !SQLITECIPHER && !NO_SQLITEPCL_RAW_BATTERIES
file sealed class WinClauseRow
{
    [Key]
    public int Id { get; set; }

    public int Bucket { get; set; }

    public double Amount { get; set; }

    public string Name { get; set; } = "";
}

file sealed class WinClauseResult
{
    public int Id { get; set; }

    public double Total { get; set; }
}

file sealed class WinLabeledResult
{
    public double Total { get; set; }

    public string Label { get; set; } = "";
}

public class WindowClausePlacementTests
{
    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<WinClauseRow>().Schema.CreateTable();
        db.Table<WinClauseRow>().Add(new WinClauseRow { Id = 1, Bucket = 1, Amount = 10 });
        db.Table<WinClauseRow>().Add(new WinClauseRow { Id = 2, Bucket = 1, Amount = 20 });
        db.Table<WinClauseRow>().Add(new WinClauseRow { Id = 3, Bucket = 2, Amount = 30 });
        return db;
    }

    [Fact]
    public void PartitionByAfterThenPartitionByThrows()
    {
        using TestDatabase db = Setup();
        Assert.Throws<NotSupportedException>(() =>
            db.Table<WinClauseRow>()
                .Select(r => new WinClauseResult
                {
                    Id = r.Id,
                    Total = SQLiteWindowFunctions.Sum(r.Amount).PartitionBy(r.Bucket).ThenPartitionBy(r.Id).PartitionBy(r.Bucket)
                })
                .ToList());
    }

    [Fact]
    public void PartitionByAfterOrderByThrows()
    {
        using TestDatabase db = Setup();
        Assert.Throws<NotSupportedException>(() =>
            db.Table<WinClauseRow>()
                .Select(r => new WinClauseResult
                {
                    Id = r.Id,
                    Total = SQLiteWindowFunctions.Sum(r.Amount).OrderBy(r.Amount).PartitionBy(r.Bucket)
                })
                .ToList());
    }

    [Fact]
    public void PartitionByAfterOrderByDescendingThrows()
    {
        using TestDatabase db = Setup();
        Assert.Throws<NotSupportedException>(() =>
            db.Table<WinClauseRow>()
                .Select(r => new WinClauseResult
                {
                    Id = r.Id,
                    Total = SQLiteWindowFunctions.Sum(r.Amount).OrderByDescending(r.Amount).PartitionBy(r.Bucket)
                })
                .ToList());
    }

    [Fact]
    public void PartitionByAfterThenOrderByThrows()
    {
        using TestDatabase db = Setup();
        Assert.Throws<NotSupportedException>(() =>
            db.Table<WinClauseRow>()
                .Select(r => new WinClauseResult
                {
                    Id = r.Id,
                    Total = SQLiteWindowFunctions.Sum(r.Amount).OrderBy(r.Amount).ThenOrderBy(r.Id).PartitionBy(r.Bucket)
                })
                .ToList());
    }

    [Fact]
    public void PartitionByAfterThenOrderByDescendingThrows()
    {
        using TestDatabase db = Setup();
        Assert.Throws<NotSupportedException>(() =>
            db.Table<WinClauseRow>()
                .Select(r => new WinClauseResult
                {
                    Id = r.Id,
                    Total = SQLiteWindowFunctions.Sum(r.Amount).OrderBy(r.Amount).ThenOrderByDescending(r.Id).PartitionBy(r.Bucket)
                })
                .ToList());
    }

    [Fact]
    public void BareWindowFunctionAggregatesAllRows()
    {
        using TestDatabase db = Setup();

        double total = db.Table<WinClauseRow>().AsEnumerable().Sum(r => r.Amount);
        Assert.Equal(60.0, total);

        List<WinClauseResult> actual = db.Table<WinClauseRow>()
            .OrderBy(r => r.Id)
            .Select(r => new WinClauseResult { Id = r.Id, Total = SQLiteWindowFunctions.Sum(r.Amount) })
            .ToList();

        Assert.All(actual, x => Assert.Equal(total, x.Total));
    }

    [Fact]
    public void WindowFunctionAlongsideStringMethodProjects()
    {
        using TestDatabase db = Setup();
        db.Table<WinClauseRow>().Update(new WinClauseRow { Id = 1, Bucket = 1, Amount = 10, Name = "ab" });

        double total = db.Table<WinClauseRow>().AsEnumerable().Sum(r => r.Amount);

        List<WinLabeledResult> actual = db.Table<WinClauseRow>()
            .OrderBy(r => r.Id)
            .Select(r => new WinLabeledResult
            {
                Total = SQLiteWindowFunctions.Sum(r.Amount).Over().OrderBy(r.Id),
                Label = r.Name.ToUpper()
            })
            .ToList();

        Assert.Equal(3, actual.Count);
        Assert.Equal(total, actual[^1].Total);
    }
}
#endif
