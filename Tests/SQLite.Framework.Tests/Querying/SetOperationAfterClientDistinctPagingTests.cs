using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26pPagedRows")]
public class H26pPagedRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class SetOperationAfterClientDistinctPagingTests
{
    [Fact]
    public void UnionAfterATakeOnADistinctClientProjectionIsRejected()
    {
        using TestDatabase db = Setup(nameof(UnionAfterATakeOnADistinctClientProjectionIsRejected));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<H26pPagedRow>()
            .Select(r => new { r.Id, Tags = new[] { r.A } })
            .Distinct()
            .Take(2)
            .Union(db.Table<H26pPagedRow>().Select(r => new { r.Id, Tags = new[] { r.A } }))
            .ToList());

        Assert.Contains("after OrderBy, Take or Skip", ex.Message);
    }

    [Fact]
    public void UnionAfterASkipOnADistinctClientProjectionIsRejected()
    {
        using TestDatabase db = Setup(nameof(UnionAfterASkipOnADistinctClientProjectionIsRejected));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<H26pPagedRow>()
            .Select(r => new { r.Id, Tags = new[] { r.A } })
            .Distinct()
            .Skip(1)
            .Union(db.Table<H26pPagedRow>().Select(r => new { r.Id, Tags = new[] { r.A } }))
            .ToList());

        Assert.Contains("after OrderBy, Take or Skip", ex.Message);
    }

    private static List<H26pPagedRow> Rows()
    {
        return
        [
            new H26pPagedRow { Id = 1, A = 10 },
            new H26pPagedRow { Id = 2, A = 20 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26pPagedRow>().Schema.CreateTable();
        db.Table<H26pPagedRow>().AddRange(Rows());
        return db;
    }
}
