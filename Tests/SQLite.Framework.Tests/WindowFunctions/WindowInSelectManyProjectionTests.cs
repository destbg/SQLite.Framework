using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21gCrossEvents")]
public class H21gCrossEvent
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

[Table("H21gCrossTags")]
public class H21gCrossTag
{
    [Key]
    public int Id { get; set; }

    public string Label { get; set; } = "";
}

public class H21gCrossProjection
{
    public int Id { get; set; }

    public long Rn { get; set; }
}

public class WindowInSelectManyProjectionTests
{
    [Fact]
    public void RowNumberInFusedSelectManyProjectionFiltersTopRows()
    {
        using TestDatabase db = Setup();

        List<int> expected = ExpectedTopIds();

        IQueryable<H21gCrossProjection> projected =
            from a in db.Table<H21gCrossEvent>()
            from t in db.Table<H21gCrossTag>()
            select new H21gCrossProjection
            {
                Id = a.Id,
                Rn = SQLiteWindowFunctions.RowNumber().Over().OrderByDescending(a.Amount).AsValue()
            };

        List<int> actual = projected
            .Where(r => r.Rn <= 2)
            .Select(r => r.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RowNumberInSeparateSelectAfterSelectManyFiltersTopRows()
    {
        using TestDatabase db = Setup();

        List<int> expected = ExpectedTopIds();

        List<int> actual = db.Table<H21gCrossEvent>()
            .SelectMany(_ => db.Table<H21gCrossTag>(), (a, t) => new { a.Id, a.Amount })
            .Select(r => new H21gCrossProjection
            {
                Id = r.Id,
                Rn = SQLiteWindowFunctions.RowNumber().Over().OrderByDescending(r.Amount).AsValue()
            })
            .Where(r => r.Rn <= 2)
            .Select(r => r.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<int> ExpectedTopIds()
    {
        return Events()
            .SelectMany(_ => Tags(), (a, t) => new { a.Id, a.Amount })
            .OrderByDescending(r => r.Amount)
            .Select((r, i) => new { r.Id, Rn = (long)i + 1 })
            .Where(r => r.Rn <= 2)
            .Select(r => r.Id)
            .OrderBy(v => v)
            .ToList();
    }

    private static List<H21gCrossEvent> Events()
    {
        return
        [
            new H21gCrossEvent { Id = 1, Amount = 40 },
            new H21gCrossEvent { Id = 2, Amount = 30 },
            new H21gCrossEvent { Id = 3, Amount = 20 },
            new H21gCrossEvent { Id = 4, Amount = 10 }
        ];
    }

    private static List<H21gCrossTag> Tags()
    {
        return [new H21gCrossTag { Id = 1, Label = "x" }];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21gCrossEvent>().Schema.CreateTable();
        db.Table<H21gCrossTag>().Schema.CreateTable();
        db.Table<H21gCrossEvent>().AddRange(Events());
        db.Table<H21gCrossTag>().AddRange(Tags());
        return db;
    }
}
