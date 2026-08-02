using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ChnRefillDocs")]
public class ChnRefillDoc
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(ChnRefillDoc), AutoSync = FtsAutoSync.Triggers)]
[Table("ChnRefillSearch")]
public class ChnRefillSearchRow
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(ChnRefillDoc), AutoSync = FtsAutoSync.Manual)]
[Table("ChnRefillManual")]
public class ChnRefillManualRow
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class MigrationFtsRefillScopeTests
{
    [Fact]
    public void AContentRebuildRefillsOnlyTheSearchTableThatReferencesIt()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"ChnRefillDocs\" (\"Id\" INTEGER PRIMARY KEY, \"Body\" TEXT NOT NULL, \"Legacy\" TEXT)");
        db.Table<ChnRefillSearchRow>().Schema.CreateTable();
        db.Table<ChnRefillDoc>().Add(new ChnRefillDoc { Id = 1, Body = "alpha" });
        db.Execute("CREATE VIRTUAL TABLE \"ChnRefillOther\" USING fts5(\"Body\", tokenize='unicode61')");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<ChnRefillDoc>(rebuild: true))
            .Migrate();

        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnRefillSearch\" WHERE \"ChnRefillSearch\" MATCH 'alpha'"));
        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnRefillOther\""));
    }

    [Fact]
    public void AContentRebuildDoesNotRefillAManualSyncSearchTable()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"ChnRefillDocs\" (\"Id\" INTEGER PRIMARY KEY, \"Body\" TEXT NOT NULL, \"Legacy\" TEXT)");
        db.Table<ChnRefillSearchRow>().Schema.CreateTable();
        db.Table<ChnRefillManualRow>().Schema.CreateTable();
        db.Table<ChnRefillDoc>().Add(new ChnRefillDoc { Id = 1, Body = "alpha" });
        db.Execute("INSERT INTO \"ChnRefillManual\"(rowid, \"Body\") VALUES (1, 'alpha')");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<ChnRefillDoc>(rebuild: true))
            .Migrate();

        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnRefillSearch\" WHERE \"ChnRefillSearch\" MATCH 'alpha'"));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnRefillManual\" WHERE \"ChnRefillManual\" MATCH 'alpha'"));
    }
}
