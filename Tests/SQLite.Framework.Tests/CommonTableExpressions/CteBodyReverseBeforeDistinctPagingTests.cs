using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26bPagedRows")]
public class H26bPagedRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class CteBodyReverseBeforeDistinctPagingTests
{
    [Fact]
    public void ACteBodyThatReversesBeforeDistinctAndTakesIsRejected()
    {
        using TestDatabase db = Setup(nameof(ACteBodyThatReversesBeforeDistinctAndTakesIsRejected));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.With(() => db.Table<H26bPagedRow>()
                .OrderBy(r => r.Id)
                .Select(r => new { r.Id, Tags = new[] { r.A } })
                .Reverse()
                .Distinct()
                .Take(2))
            .Select(x => x.Id)
            .ToList());

        Assert.Contains("Reverse", ex.Message, StringComparison.Ordinal);
    }

    private static List<H26bPagedRow> Rows()
    {
        return
        [
            new H26bPagedRow { Id = 1, A = 10 },
            new H26bPagedRow { Id = 2, A = 20 },
            new H26bPagedRow { Id = 3, A = 30 },
            new H26bPagedRow { Id = 4, A = 40 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26bPagedRow>().Schema.CreateTable();
        db.Table<H26bPagedRow>().AddRange(Rows());
        return db;
    }
}
