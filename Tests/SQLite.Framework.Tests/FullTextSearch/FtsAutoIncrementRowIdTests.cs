using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
[Table("H24oAutoNoteSearch")]
public class H24oAutoNoteSearch
{
    [FullTextRowId]
    [AutoIncrement]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class FtsAutoIncrementRowIdTests
{
    [Fact]
    public void AutoIncrementRowIdLetsSqliteAssignDistinctRowIds()
    {
        using TestDatabase db = new();
        db.Table<H24oAutoNoteSearch>().Schema.CreateTable();

        H24oAutoNoteSearch first = new() { Body = "alpha" };
        H24oAutoNoteSearch second = new() { Body = "beta" };
        db.Table<H24oAutoNoteSearch>().Add(first);
        db.Table<H24oAutoNoteSearch>().Add(second);

        List<string> expected = ["alpha", "beta"];
        List<string> actual = db.Table<H24oAutoNoteSearch>()
            .OrderBy(n => n.Id)
            .Select(n => n.Body)
            .ToList();

        Assert.Equal(expected, actual);
        Assert.NotEqual(first.Id, second.Id);
    }
}
