using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CapturedListComputedRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public class CapturedListComputedArgumentIndexOfTests
{
    private static List<CapturedListComputedRow> Rows() =>
    [
        new() { Id = 1, Amount = 3 },
        new() { Id = 2, Amount = 5 },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<CapturedListComputedRow>().Schema.CreateTable();
        db.Table<CapturedListComputedRow>().AddRange(Rows());
        return db;
    }

    private static int StartFor(int id)
    {
        return id - 1;
    }

    [Fact]
    public void IndexOfProductInSelectProjects()
    {
        using TestDatabase db = Seed();
        List<int> values = [6, 10, 20, 12];

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => values.IndexOf(r.Amount * 2))
            .ToList();
        Assert.Equal([0, 1], expected);

        List<int> actual = db.Table<CapturedListComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => values.IndexOf(r.Amount * 2))
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IndexOfProductWithStaticHelperStartInSelectProjects()
    {
        using TestDatabase db = Seed();
        List<int> values = [6, 10, 20, 12];

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => values.IndexOf(r.Amount * 2, StartFor(r.Id)))
            .ToList();
        Assert.Equal([0, 1], expected);

        List<int> actual = db.Table<CapturedListComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => values.IndexOf(r.Amount * 2, StartFor(r.Id)))
            .ToList();
        Assert.Equal(expected, actual);
    }
}
