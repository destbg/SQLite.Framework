using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25rContentlessNotes")]
[FullTextSearch(ContentMode = FtsContentMode.Contentless)]
public class H25rContentlessNote
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Title { get; set; } = "";

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class ContentlessFullTextIndexReplaceTests
{
    [Fact]
    public void AddOrUpdateOverAContentlessIndexIsRefusedAndKeepsTheIndexIntact()
    {
        using TestDatabase db = new();
        db.Table<H25rContentlessNote>().Schema.CreateTable();
        db.Table<H25rContentlessNote>().Add(new H25rContentlessNote { Id = 1, Title = "apple", Body = "pie" });

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<H25rContentlessNote>().AddOrUpdate(new H25rContentlessNote { Id = 1, Title = "cherry", Body = "tart" }));

        Assert.Contains("contentless", ex.Message);
        List<int> actual = db.Table<H25rContentlessNote>()
            .Where(n => SQLiteFTS5Functions.Match(n, "apple"))
            .OrderBy(n => n.Id)
            .Select(n => n.Id)
            .ToList();

        Assert.Equal(new List<int> { 1 }, actual);
    }

    [Fact]
    public void AddOrUpdateRangeOverAContentlessIndexIsRefusedAndKeepsTheIndexIntact()
    {
        using TestDatabase db = new();
        db.Table<H25rContentlessNote>().Schema.CreateTable();
        db.Table<H25rContentlessNote>().Add(new H25rContentlessNote { Id = 1, Title = "apple", Body = "pie" });

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<H25rContentlessNote>().AddOrUpdateRange(
                [new H25rContentlessNote { Id = 1, Title = "cherry", Body = "tart" }]));

        Assert.Contains("contentless", ex.Message);
        List<int> actual = db.Table<H25rContentlessNote>()
            .Where(n => SQLiteFTS5Functions.Match(n, "pie"))
            .OrderBy(n => n.Id)
            .Select(n => n.Id)
            .ToList();

        Assert.Equal(new List<int> { 1 }, actual);
    }
}
