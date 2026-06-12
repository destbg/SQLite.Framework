using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class NullableSumFrameRow
{
    [Key]
    public int Id { get; set; }

    public int? Amount { get; set; }
}

public class WindowNullableSumEmptyFrameTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<NullableSumFrameRow>().Schema.CreateTable();
        db.Table<NullableSumFrameRow>().Add(new NullableSumFrameRow { Id = 1, Amount = 5 });
        db.Table<NullableSumFrameRow>().Add(new NullableSumFrameRow { Id = 2, Amount = null });
        db.Table<NullableSumFrameRow>().Add(new NullableSumFrameRow { Id = 3, Amount = 9 });
        return db;
    }

    [Fact]
    public void SumOverEmptyFrameReturnsZero()
    {
        using TestDatabase db = SetupDatabase();

        List<NullableSumFrameRow> rows = db.Table<NullableSumFrameRow>().AsEnumerable().OrderBy(r => r.Id).ToList();

        List<int?> expected = rows
            .Select((r, index) => rows.Skip(index + 2).Take(2).Sum(x => x.Amount))
            .ToList();

        Assert.Equal([9, 0, 0], expected);

        List<int?> actual = db.Table<NullableSumFrameRow>()
            .Select(r => new
            {
                r.Id,
                Total = SQLiteWindowFunctions.Sum(r.Amount)
                    .OrderBy(r.Id)
                    .Rows(SQLiteFrameBoundary.Following(2), SQLiteFrameBoundary.Following(3))
                    .AsValue()
            })
            .OrderBy(x => x.Id)
            .AsEnumerable()
            .Select(x => x.Total)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumWithFilterExcludingEveryRowReturnsZero()
    {
        using TestDatabase db = SetupDatabase();

        List<NullableSumFrameRow> rows = db.Table<NullableSumFrameRow>().AsEnumerable().OrderBy(r => r.Id).ToList();

        List<int?> expected = rows
            .Select(_ => rows.Where(x => x.Id > 1000).Sum(x => x.Amount))
            .ToList();

        Assert.Equal([0, 0, 0], expected);

        List<int?> actual = db.Table<NullableSumFrameRow>()
            .Select(r => new { r.Id, Total = SQLiteWindowFunctions.Sum(r.Amount).Filter(r.Id > 1000).AsValue() })
            .OrderBy(x => x.Id)
            .AsEnumerable()
            .Select(x => x.Total)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
