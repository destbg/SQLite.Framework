using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ExternalContentFtsRenamedRowIdColumnTests
{
    [Fact]
    public void InsertSyncsThroughRenamedRowIdColumn()
    {
        using TestDatabase db = new();
        db.Table<FtsRowIdArticle>().Schema.CreateTable();
        db.Table<FtsRowIdArticleSearch>().Schema.CreateTable();

        db.Table<FtsRowIdArticle>().Add(new FtsRowIdArticle { Slug = 42, Body = "hello world" });

        long matches = db.Table<FtsRowIdArticleSearch>()
            .LongCount(s => SQLiteFTS5Functions.Match(s, "hello"));

        Assert.Equal(1, matches);
    }
}

public class FtsRowIdArticle
{
    [Key]
    [Column("slug_id")]
    public int Slug { get; set; }

    public required string Body { get; set; }
}

[FullTextSearch(
    ContentMode = FtsContentMode.External,
    ContentTable = typeof(FtsRowIdArticle),
    ContentRowIdColumn = nameof(FtsRowIdArticle.Slug),
    AutoSync = FtsAutoSync.Triggers)]
public class FtsRowIdArticleSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
