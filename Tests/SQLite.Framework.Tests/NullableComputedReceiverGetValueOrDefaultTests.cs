using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NullableComputedRow
{
    [Key]
    public int Id { get; set; }

    public int? Amount { get; set; }
}

public class NullableComputedReceiverGetValueOrDefaultTests
{
    private static List<NullableComputedRow> Rows() =>
    [
        new() { Id = 1, Amount = 3 },
        new() { Id = 2, Amount = null },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<NullableComputedRow>().Schema.CreateTable();
        db.Table<NullableComputedRow>().AddRange(Rows());
        return db;
    }

    private static int FallbackFor(int id)
    {
        return id * 100;
    }

    [Fact]
    public void GetValueOrDefaultStaticHelperOnProductInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Amount * 2).GetValueOrDefault(FallbackFor(r.Id)))
            .ToList();
        Assert.Equal([6, 200], expected);

        List<int> actual = db.Table<NullableComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => (r.Amount * 2).GetValueOrDefault(FallbackFor(r.Id)))
            .ToList();
        Assert.Equal(expected, actual);
    }
}
