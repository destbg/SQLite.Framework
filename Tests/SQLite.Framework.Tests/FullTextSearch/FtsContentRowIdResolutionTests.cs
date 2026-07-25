using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FtsColNameArticle
{
    [Key]
    [Column("slug_id")]
    public int Slug { get; set; }

    public required string Body { get; set; }
}

[FullTextSearch(
    ContentMode = FtsContentMode.External,
    ContentTable = typeof(FtsColNameArticle),
    ContentRowIdColumn = "slug_id",
    AutoSync = FtsAutoSync.Triggers)]
public class FtsColNameSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}

[FullTextSearch(
    ContentMode = FtsContentMode.External,
    ContentTable = typeof(FtsColNameArticle),
    ContentRowIdColumn = "no_such_column",
    AutoSync = FtsAutoSync.Manual)]
public class FtsUnknownRowIdSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}

public class FtsContentRowIdResolutionTests
{
    [Fact]
    public void RowIdResolvedByColumnNameSyncs()
    {
        using TestDatabase db = new();
        db.Table<FtsColNameArticle>().Schema.CreateTable();
        db.Table<FtsColNameSearch>().Schema.CreateTable();

        db.Table<FtsColNameArticle>().Add(new FtsColNameArticle { Slug = 42, Body = "hello world" });

        long matches = db.Table<FtsColNameSearch>()
            .LongCount(s => SQLiteFTS5Functions.Match(s, "hello"));

        Assert.Equal(1, matches);
    }

    [Fact]
    public void RowIdUnknownNameIsPassedThrough()
    {
        using TestDatabase db = new();
        db.Table<FtsColNameArticle>().Schema.CreateTable();

        db.Table<FtsUnknownRowIdSearch>().Schema.CreateTable();

        long created = db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE name = 'FtsUnknownRowIdSearch'");
        Assert.Equal(1, created);
    }
}
