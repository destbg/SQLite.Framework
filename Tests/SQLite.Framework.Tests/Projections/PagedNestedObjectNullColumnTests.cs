using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25bPagedNullRows")]
public class H25bPagedNullRow
{
    [Key]
    public int Id { get; set; }

    public string? Note { get; set; }

    public int? Amount { get; set; }
}

public class H25bPagedNullChild
{
    public string? Note { get; set; }

    public int? Amount { get; set; }
}

public class H25bPagedNullOuter
{
    public int Id { get; set; }

    public H25bPagedNullChild Child { get; set; } = null!;
}

public class PagedNestedObjectNullColumnTests
{
    [Fact]
    public void ANestedObjectStaysNonNullWhenEveryColumnIsNullAfterTakeAndDistinct()
    {
        using TestDatabase db = Setup(nameof(ANestedObjectStaysNonNullWhenEveryColumnIsNullAfterTakeAndDistinct));

        List<bool> expected = Rows()
            .Select(r => new H25bPagedNullOuter { Id = r.Id, Child = new H25bPagedNullChild { Note = r.Note, Amount = r.Amount } })
            .Take(2)
            .Distinct()
            .OrderBy(o => o.Id)
            .Select(o => o.Child != null)
            .ToList();

        Assert.Equal(new List<bool> { true, true }, expected);

        List<bool> actual = db.Table<H25bPagedNullRow>()
            .Select(r => new H25bPagedNullOuter { Id = r.Id, Child = new H25bPagedNullChild { Note = r.Note, Amount = r.Amount } })
            .Take(2)
            .Distinct()
            .AsEnumerable()
            .OrderBy(o => o.Id)
            .Select(o => o.Child != null)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25bPagedNullRow> Rows()
    {
        return
        [
            new H25bPagedNullRow { Id = 1, Note = null, Amount = null },
            new H25bPagedNullRow { Id = 2, Note = "x", Amount = 5 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25bPagedNullRow>().Schema.CreateTable();
        db.Table<H25bPagedNullRow>().AddRange(Rows());
        return db;
    }
}
