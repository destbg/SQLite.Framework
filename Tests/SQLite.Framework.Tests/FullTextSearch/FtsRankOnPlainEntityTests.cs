using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24oPlainDocs")]
public class H24oPlainDoc
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

public class FtsRankOnPlainEntityTests
{
    [Fact]
    public void RankOnEntityWithoutFullTextSearchAttributeThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<H24oPlainDoc>().Schema.CreateTable();
        db.Table<H24oPlainDoc>().Add(new H24oPlainDoc { Id = 1, Body = "hello" });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<H24oPlainDoc>()
                .OrderBy(d => SQLiteFTS5Functions.Rank(d))
                .ToList());
    }
}
