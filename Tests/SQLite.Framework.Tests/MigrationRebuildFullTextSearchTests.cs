using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FtsRebuildItems")]
public class FtsRebuildItem
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(FtsRebuildItem), AutoSync = FtsAutoSync.Triggers)]
[Table("FtsRebuildSearch")]
public class FtsRebuildSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

[FullTextSearch]
[Table("FtsRebuildSelfContained")]
public class FtsRebuildSelfContained
{
    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class MigrationRebuildFullTextSearchTests
{
    [Fact]
    public void RebuildRefillsIndexFromContentTable()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<FtsRebuildItem>().Schema.CreateTable();
        db.Table<FtsRebuildItem>().Add(new FtsRebuildItem { Id = 1, Body = "hello world" });
        db.Table<FtsRebuildItem>().Add(new FtsRebuildItem { Id = 2, Body = "quiet river" });

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<FtsRebuildSearch>().RebuildFullTextSearch<FtsRebuildSearch>())
            .Migrate();

        long matches = db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"FtsRebuildSearch\" WHERE \"FtsRebuildSearch\" MATCH 'hello'");
        Assert.Equal(1, matches);
    }

    [Fact]
    public void RebuildRecreatesSyncTriggers()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<FtsRebuildItem>().Schema.CreateTable();

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<FtsRebuildSearch>().RebuildFullTextSearch<FtsRebuildSearch>())
            .Migrate();
        db.Table<FtsRebuildItem>().Add(new FtsRebuildItem { Id = 5, Body = "fresh entry" });

        long matches = db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"FtsRebuildSearch\" WHERE \"FtsRebuildSearch\" MATCH 'fresh'");
        Assert.Equal(1, matches);
    }

    [Fact]
    public void NonFtsEntityThrows()
    {
        using TestDatabase db = new(useFile: true);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Schema.Migrations()
            .Version(1, m => m.RebuildFullTextSearch<FtsRebuildItem>())
            .Migrate());

        Assert.Equal("'FtsRebuildItem' is not an FTS5 entity. Mark it with [FullTextSearch] or remove this step.", ex.Message);
    }

    [Fact]
    public void FtsWithoutContentTableThrows()
    {
        using TestDatabase db = new(useFile: true);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Schema.Migrations()
            .Version(1, m => m.RebuildFullTextSearch<FtsRebuildSelfContained>())
            .Migrate());

        Assert.Equal("'FtsRebuildSelfContained' has no content table, so its rows cannot be rebuilt. Set ContentTable on [FullTextSearch] or move the data with Run callbacks.", ex.Message);
    }

    [Fact]
    public void PlanDescribesRebuild()
    {
        using TestDatabase db = new(useFile: true);

        SQLiteMigrationPlan plan = db.Schema.Migrations()
            .Version(1, m => m.RebuildFullTextSearch<FtsRebuildSearch>())
            .Plan();

        Assert.Equal(["rebuild full text search \"FtsRebuildSearch\""], plan.Operations);
    }
}
