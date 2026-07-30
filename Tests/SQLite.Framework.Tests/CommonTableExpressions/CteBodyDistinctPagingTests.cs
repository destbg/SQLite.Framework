using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25dPageRows")]
public class H25dPageRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class CteBodyDistinctPagingTests
{
    [Fact]
    public void ACteBodyThatTakesAfterDistinctKeepsOnlyTheTakenRows()
    {
        using TestDatabase db = Setup(nameof(ACteBodyThatTakesAfterDistinctKeepsOnlyTheTakenRows));

        int expected = Rows()
            .Select(r => new { r.Id, Tags = new[] { r.A } })
            .Distinct()
            .Take(2)
            .Count();

        int actual = db.With(() => db.Table<H25dPageRow>()
                .Select(r => new { r.Id, Tags = new[] { r.A } })
                .Distinct()
                .Take(2))
            .Select(x => x.Id)
            .ToList()
            .Count;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ACteBodyThatSkipsAfterDistinctDropsTheSkippedRows()
    {
        using TestDatabase db = Setup(nameof(ACteBodyThatSkipsAfterDistinctDropsTheSkippedRows));

        int expected = Rows()
            .Select(r => new { r.Id, Tags = new[] { r.A } })
            .Distinct()
            .Skip(3)
            .Count();

        int actual = db.With(() => db.Table<H25dPageRow>()
                .Select(r => new { r.Id, Tags = new[] { r.A } })
                .Distinct()
                .Skip(3))
            .Select(x => x.Id)
            .ToList()
            .Count;

        Assert.Equal(expected, actual);
    }

    private static List<H25dPageRow> Rows()
    {
        return
        [
            new H25dPageRow { Id = 1, A = 10 },
            new H25dPageRow { Id = 2, A = 20 },
            new H25dPageRow { Id = 3, A = 30 },
            new H25dPageRow { Id = 4, A = 40 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25dPageRow>().Schema.CreateTable();
        db.Table<H25dPageRow>().AddRange(Rows());
        return db;
    }
}
