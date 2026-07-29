using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24bNullCheckRows")]
public class H24bNullCheckRow
{
    [Key]
    public int Id { get; set; }

    public string? Note { get; set; }
}

public class H24bNullCheckSide
{
    public string? Note { get; set; }
}

public class H24bNullCheckOuter
{
    public int Id { get; set; }

    public H24bNullCheckSide? Side { get; set; }
}

public class ConstructedNestedObjectNullComparisonTests
{
    [Fact]
    public void NestedConstructedMemberOfANamedOuterIsNeverEqualToNull()
    {
        using TestDatabase db = Setup(nameof(NestedConstructedMemberOfANamedOuterIsNeverEqualToNull));

        List<int> expected = Rows()
            .Select(r => new H24bNullCheckOuter { Id = r.Id, Side = new H24bNullCheckSide { Note = r.Note } })
            .Where(o => o.Side == null)
            .Select(o => o.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H24bNullCheckRow>()
            .Select(r => new H24bNullCheckOuter { Id = r.Id, Side = new H24bNullCheckSide { Note = r.Note } })
            .Where(o => o.Side == null)
            .Select(o => o.Id)
            .AsEnumerable()
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedConstructedMemberOfANamedOuterIsAlwaysDifferentFromNull()
    {
        using TestDatabase db = Setup(nameof(NestedConstructedMemberOfANamedOuterIsAlwaysDifferentFromNull));

        List<int> expected = Rows()
            .Select(r => new H24bNullCheckOuter { Id = r.Id, Side = new H24bNullCheckSide { Note = r.Note } })
            .Where(o => o.Side != null)
            .Select(o => o.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H24bNullCheckRow>()
            .Select(r => new H24bNullCheckOuter { Id = r.Id, Side = new H24bNullCheckSide { Note = r.Note } })
            .Where(o => o.Side != null)
            .Select(o => o.Id)
            .AsEnumerable()
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedConstructedMemberOfAnAnonymousOuterIsAlwaysDifferentFromNull()
    {
        using TestDatabase db = Setup(nameof(NestedConstructedMemberOfAnAnonymousOuterIsAlwaysDifferentFromNull));

        List<int> expected = Rows()
            .Select(r => new { r.Id, Side = new H24bNullCheckSide { Note = r.Note } })
            .Where(o => o.Side != null)
            .Select(o => o.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H24bNullCheckRow>()
            .Select(r => new { r.Id, Side = new H24bNullCheckSide { Note = r.Note } })
            .Where(o => o.Side != null)
            .Select(o => o.Id)
            .AsEnumerable()
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24bNullCheckRow> Rows()
    {
        return
        [
            new H24bNullCheckRow { Id = 1, Note = "alpha" },
            new H24bNullCheckRow { Id = 2, Note = null },
            new H24bNullCheckRow { Id = 3, Note = "gamma" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(methodName);
        db.Table<H24bNullCheckRow>().Schema.CreateTable();
        db.Table<H24bNullCheckRow>().AddRange(Rows());
        return db;
    }
}
