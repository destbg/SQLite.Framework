using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24iPagedRows")]
public class H24iPagedRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H24iPagedBox
{
    public int Id { get; set; }
}

public class ClientPagedSetOperationTests
{
    [Fact]
    public void TakeOnADistinctInMemoryProjectionBeforeConcatThrows()
    {
        using TestDatabase db = Setup();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<H24iPagedRow>().Where(r => r.Id <= 2)
                .Select(r => r.A > 0 ? new H24iPagedBox { Id = r.Id } : new H24iPagedBox { Id = r.A })
                .Distinct()
                .Take(1)
                .Concat(db.Table<H24iPagedRow>().Where(r => r.Id >= 3)
                    .Select(r => r.A > 0 ? new H24iPagedBox { Id = r.Id } : new H24iPagedBox { Id = r.A }))
                .ToList());
    }

    [Fact]
    public void SkipOnADistinctInMemoryProjectionBeforeConcatThrows()
    {
        using TestDatabase db = Setup();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<H24iPagedRow>().Where(r => r.Id <= 2)
                .Select(r => r.A > 0 ? new H24iPagedBox { Id = r.Id } : new H24iPagedBox { Id = r.A })
                .Distinct()
                .Skip(1)
                .Concat(db.Table<H24iPagedRow>().Where(r => r.Id >= 3)
                    .Select(r => r.A > 0 ? new H24iPagedBox { Id = r.Id } : new H24iPagedBox { Id = r.A }))
                .ToList());
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H24iPagedRow>().Schema.CreateTable();
        db.Table<H24iPagedRow>().AddRange(Rows());
        return db;
    }

    private static List<H24iPagedRow> Rows()
    {
        return
        [
            new H24iPagedRow { Id = 1, A = 10 },
            new H24iPagedRow { Id = 2, A = 20 },
            new H24iPagedRow { Id = 3, A = 30 },
            new H24iPagedRow { Id = 4, A = 40 }
        ];
    }
}
