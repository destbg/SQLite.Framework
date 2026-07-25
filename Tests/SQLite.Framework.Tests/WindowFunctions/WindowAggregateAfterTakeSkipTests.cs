using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class WindowPageRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class WindowAggregateAfterTakeSkipTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<WindowPageRow>().Schema.CreateTable();
        for (int i = 1; i <= 6; i++)
        {
            db.Table<WindowPageRow>().Add(new WindowPageRow { Id = i, Value = i * 10 });
        }
        return db;
    }

    [Fact]
    public void CountOverTakenRowsSeesOnlyTakenRows()
    {
        using TestDatabase db = SetupDatabase();

        List<WindowPageRow> taken = db.Table<WindowPageRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Take(3)
            .ToList();

        List<long> expected = taken.Select(_ => (long)taken.Count).ToList();

        Assert.Equal([3, 3, 3], expected);

        List<long> actual = db.Table<WindowPageRow>()
            .OrderBy(r => r.Id)
            .Take(3)
            .Select(r => SQLiteWindowFunctions.Count().AsValue())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumOverTakenRowsSeesOnlyTakenRows()
    {
        using TestDatabase db = SetupDatabase();

        List<WindowPageRow> taken = db.Table<WindowPageRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Take(3)
            .ToList();

        List<int> expected = taken.Select(_ => taken.Sum(x => x.Value)).ToList();

        Assert.Equal([60, 60, 60], expected);

        List<int> actual = db.Table<WindowPageRow>()
            .OrderBy(r => r.Id)
            .Take(3)
            .Select(r => SQLiteWindowFunctions.Sum(r.Value).AsValue())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountOverSkippedRowsSeesOnlyRemainingRows()
    {
        using TestDatabase db = SetupDatabase();

        List<WindowPageRow> remaining = db.Table<WindowPageRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Skip(2)
            .ToList();

        List<long> expected = remaining.Select(_ => (long)remaining.Count).ToList();

        Assert.Equal([4, 4, 4, 4], expected);

        List<long> actual = db.Table<WindowPageRow>()
            .OrderBy(r => r.Id)
            .Skip(2)
            .Select(r => SQLiteWindowFunctions.Count().AsValue())
            .ToList();

        Assert.Equal(expected, actual);
    }
}
