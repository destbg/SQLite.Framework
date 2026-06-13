using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class WindowRankedRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class WindowProjectionInWherePredicateTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<WindowRankedRow>().Schema.CreateTable();
        for (int i = 1; i <= 6; i++)
        {
            db.Table<WindowRankedRow>().Add(new WindowRankedRow { Id = i, Value = i * 10 });
        }
        return db;
    }

    [Fact]
    public void RowNumberProjectionFilteredInWhereReturnsTopRows()
    {
        using TestDatabase db = SetupDatabase();

        List<WindowRankedRow> rows = db.Table<WindowRankedRow>().AsEnumerable().ToList();

        var expected = rows
            .OrderByDescending(r => r.Value)
            .Select((r, index) => new { r.Id, Rn = (long)index + 1 })
            .Where(r => r.Rn <= 2)
            .OrderBy(r => r.Id)
            .ToList();

        Assert.Equal([5, 6], expected.Select(r => r.Id));
        Assert.Equal([2, 1], expected.Select(r => r.Rn));

        var actual = db.Table<WindowRankedRow>()
            .Select(r => new { r.Id, Rn = SQLiteWindowFunctions.RowNumber().OrderByDescending(r.Value).AsValue() })
            .Where(r => r.Rn <= 2)
            .OrderBy(r => r.Id)
            .ToList();

        Assert.Equal(expected.Select(r => r.Id), actual.Select(r => r.Id));
        Assert.Equal(expected.Select(r => r.Rn), actual.Select(r => r.Rn));
    }

    [Fact]
    public void CountWithRowNumberPredicateCountsTopRows()
    {
        using TestDatabase db = SetupDatabase();

        List<WindowRankedRow> rows = db.Table<WindowRankedRow>().AsEnumerable().ToList();

        int expected = rows
            .OrderByDescending(r => r.Value)
            .Select((r, index) => (long)index + 1)
            .Count(rn => rn <= 2);

        Assert.Equal(2, expected);

        int actual = db.Table<WindowRankedRow>()
            .Select(r => new { Rn = SQLiteWindowFunctions.RowNumber().OrderByDescending(r.Value).AsValue() })
            .Count(r => r.Rn <= 2);

        Assert.Equal(expected, actual);
    }
}
