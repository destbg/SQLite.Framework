using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecGOrphanNotes")]
public class SecGOrphanNote
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(SecGOrphanNote), AutoSync = FtsAutoSync.Triggers)]
[Table("SecGOrphanNoteSearch")]
public class SecGOrphanNoteSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class MigrationDropSearchOrphanedTriggerTests
{
    [Fact]
    public void DropTableStepByNameRemovesTheSyncTriggers()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<SecGOrphanNote>().Schema.CreateTable();
        db.Table<SecGOrphanNoteSearch>().Schema.CreateTable();
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SecGOrphanNote>())
            .Version(2, m => m.DropTable("SecGOrphanNoteSearch"))
            .Migrate();

        db.Table<SecGOrphanNote>().Add(new SecGOrphanNote { Id = 1, Body = "hello" });

        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND tbl_name = 'SecGOrphanNotes'"));
        Assert.Equal("hello", db.Table<SecGOrphanNote>().Single().Body);
    }

    [Fact]
    public void SchemaDropTableByNameRemovesTheSyncTriggers()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<SecGOrphanNote>().Schema.CreateTable();
        db.Table<SecGOrphanNoteSearch>().Schema.CreateTable();

        db.Schema.DropTable("SecGOrphanNoteSearch");
        db.Table<SecGOrphanNote>().Add(new SecGOrphanNote { Id = 1, Body = "hello" });

        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND tbl_name = 'SecGOrphanNotes'"));
        Assert.Equal("hello", db.Table<SecGOrphanNote>().Single().Body);
    }

    [Fact]
    public void DropTableByNameKeepsTriggersThatOnlyReferenceTheDroppedTable()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"SecGPlainA\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"SecGPlainB\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TRIGGER \"trg_secgplains\" AFTER INSERT ON \"SecGPlainB\" BEGIN INSERT INTO \"SecGPlainA\"(\"Id\") VALUES (new.\"Id\"); END");

        db.Schema.DropTable("SecGPlainA");

        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND name = 'trg_secgplains'"));
    }

    [Fact]
    public void DropTableByNameDoesNotDropTriggersOfASimilarlyUnderscoredTable()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"SecGaXb\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"SecGPlainC\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TRIGGER \"trg_secgaxb\" AFTER INSERT ON \"SecGPlainC\" BEGIN INSERT INTO \"SecGaXb\"(\"Id\") VALUES (new.\"Id\"); END");
        db.Execute("CREATE TABLE \"SecGaDocs\" (\"Id\" INTEGER PRIMARY KEY, \"Body\" TEXT)");
        db.Execute("CREATE VIRTUAL TABLE \"SecGa_b\" USING fts5(\"Body\", content='SecGaDocs', content_rowid='Id')");
        db.Execute("CREATE TRIGGER \"SecGa_b_sync_ad\" AFTER DELETE ON \"SecGaDocs\" BEGIN INSERT INTO \"SecGa_b\"(\"SecGa_b\", rowid, \"Body\") VALUES('delete', old.\"Id\", old.\"Body\"); END");

        db.Schema.DropTable("SecGa_b");

        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND name = 'SecGa_b_sync_ad'"));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND name = 'trg_secgaxb'"));
    }

    [Fact]
    public void DropTableByNameDoesNotDropTriggersOfASimilarlyPercentNamedTable()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"SecGPctMore\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"SecGPlainD\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TRIGGER \"trg_secgpct\" AFTER INSERT ON \"SecGPlainD\" BEGIN INSERT INTO \"SecGPctMore\"(\"Id\") VALUES (new.\"Id\"); END");
        db.Execute("CREATE TABLE \"SecGPctDocs\" (\"Id\" INTEGER PRIMARY KEY, \"Body\" TEXT)");
        db.Execute("CREATE VIRTUAL TABLE \"SecGPct%\" USING fts5(\"Body\", content='SecGPctDocs', content_rowid='Id')");
        db.Execute("CREATE TRIGGER \"SecGPct%_sync_ad\" AFTER DELETE ON \"SecGPctDocs\" BEGIN INSERT INTO \"SecGPct%\"(\"SecGPct%\", rowid, \"Body\") VALUES('delete', old.\"Id\", old.\"Body\"); END");

        db.Schema.DropTable("SecGPct%");

        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND name = 'SecGPct%_sync_ad'"));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND name = 'trg_secgpct'"));
    }
}
