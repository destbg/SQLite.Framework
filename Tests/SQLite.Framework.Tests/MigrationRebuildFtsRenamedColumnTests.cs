using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("TitledDoc")]
public class TitledDocRow
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(TitledDocRow), AutoSync = FtsAutoSync.Triggers)]
[Table("TitledDocSearch")]
public class TitledDocSearchRow
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    [Column("TitleText")]
    public string Title { get; set; } = "";
}

public class MigrationRebuildFtsRenamedColumnTests
{
    [Fact]
    public void RebuildFillsTheIndexWhenTheIndexedColumnIsRenamed()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<TitledDocRow>().Schema.CreateTable();
        db.Table<TitledDocSearchRow>().Schema.CreateTable();
        db.Table<TitledDocRow>().Add(new TitledDocRow { Id = 1, Title = "hello world" });

        db.Schema.Migrations()
            .Version(1, m => m.RebuildFullTextSearch<TitledDocSearchRow>())
            .Migrate();

        long matches = db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"TitledDocSearch\" WHERE \"TitledDocSearch\" MATCH 'hello'");
        Assert.Equal(1, matches);
    }
}
