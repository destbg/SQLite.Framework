using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ChnBDocs")]
public class ChnBDoc
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";

    public string Subtitle { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(ChnBDoc), AutoSync = FtsAutoSync.Triggers)]
[Table("ChnBDocSearch")]
public class ChnBDocSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

[Table("ChnBNotes")]
public class ChnBNote
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";

    public string Subtitle { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(ChnBNote), AutoSync = FtsAutoSync.Triggers)]
[Table("ChnBNoteSearch")]
public class ChnBNoteSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class MigrationFtsRenameRebuildTests
{
    [Fact]
    public void RebuildAfterContentAndSearchRenamesLeavesOneSetOfSyncTriggers()
    {
        using TestDatabase db = new(useFile: true);
        SeedDocInstall(db);
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(2, m => m
                .RenameTable<ChnBDoc>("ChnBDocsOld")
                .TableChanged<ChnBDoc>())
            .Version(4, m => m.RenameTable<ChnBDocSearch>("ChnBSearchOld"))
            .Version(6, m => m.RebuildFullTextSearch<ChnBDocSearch>())
            .Migrate();

        Assert.Equal(3L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND tbl_name = 'ChnBDocs'"));

        db.Table<ChnBDoc>().Add(new ChnBDoc { Id = 2, Body = "fresh", Subtitle = "newterm" });

        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnBDocSearch\" WHERE \"ChnBDocSearch\" MATCH 'hello'"));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnBDocSearch\" WHERE \"ChnBDocSearch\" MATCH 'fresh'"));
    }

    [Fact]
    public void RebuildAfterASearchRenameLeavesOneSetOfSyncTriggers()
    {
        using TestDatabase db = new(useFile: true);
        SeedNoteInstall(db);
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(2, m => m.RenameTable<ChnBNoteSearch>("ChnBNoteSearchOld"))
            .Version(4, m => m.RebuildFullTextSearch<ChnBNoteSearch>())
            .Migrate();

        Assert.Equal(3L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND tbl_name = 'ChnBNotes'"));

        db.Table<ChnBNote>().Add(new ChnBNote { Id = 2, Body = "fresh", Subtitle = "newterm" });

        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnBNoteSearch\" WHERE \"ChnBNoteSearch\" MATCH 'hello'"));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnBNoteSearch\" WHERE \"ChnBNoteSearch\" MATCH 'fresh'"));
    }

    [Fact]
    public void DropTableAfterASearchRenameDropsTheLiveSyncTriggers()
    {
        using TestDatabase db = new(useFile: true);
        SeedDocInstall(db);
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(2, m => m
                .RenameTable<ChnBDoc>("ChnBDocsOld")
                .TableChanged<ChnBDoc>())
            .Version(4, m => m.RenameTable<ChnBDocSearch>("ChnBSearchOld"))
            .Version(6, m => m.DropTable<ChnBDocSearch>())
            .Migrate();

        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND tbl_name = 'ChnBDocs'"));

        db.Table<ChnBDoc>().Add(new ChnBDoc { Id = 2, Body = "fresh", Subtitle = "newterm" });

        Assert.Equal(2L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnBDocs\""));
    }

    private static void SeedDocInstall(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnBDocsOld\" (\"Id\" INTEGER NOT NULL PRIMARY KEY, \"Body\" TEXT NOT NULL, \"Subtitle\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"ChnBDocsOld\" (\"Id\", \"Body\", \"Subtitle\") VALUES (1, 'hello', 'world')");
        db.Execute("CREATE VIRTUAL TABLE \"ChnBSearchOld\" USING fts5(\"Body\", \"Subtitle\", content='ChnBDocsOld', content_rowid='Id', tokenize='unicode61')");
        db.Execute("CREATE TRIGGER \"ChnBSearchOld_sync_ai\" AFTER INSERT ON \"ChnBDocsOld\" BEGIN INSERT INTO \"ChnBSearchOld\"(rowid, \"Body\", \"Subtitle\") VALUES (new.\"Id\", new.\"Body\", new.\"Subtitle\"); END");
        db.Execute("CREATE TRIGGER \"ChnBSearchOld_sync_ad\" AFTER DELETE ON \"ChnBDocsOld\" BEGIN INSERT INTO \"ChnBSearchOld\"(\"ChnBSearchOld\", rowid, \"Body\", \"Subtitle\") VALUES('delete', old.\"Id\", old.\"Body\", old.\"Subtitle\"); END");
        db.Execute("CREATE TRIGGER \"ChnBSearchOld_sync_au\" AFTER UPDATE ON \"ChnBDocsOld\" BEGIN INSERT INTO \"ChnBSearchOld\"(\"ChnBSearchOld\", rowid, \"Body\", \"Subtitle\") VALUES('delete', old.\"Id\", old.\"Body\", old.\"Subtitle\"); INSERT INTO \"ChnBSearchOld\"(rowid, \"Body\", \"Subtitle\") VALUES (new.\"Id\", new.\"Body\", new.\"Subtitle\"); END");
        db.Execute("INSERT INTO \"ChnBSearchOld\"(rowid, \"Body\", \"Subtitle\") SELECT \"Id\", \"Body\", \"Subtitle\" FROM \"ChnBDocsOld\"");
    }

    private static void SeedNoteInstall(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnBNotes\" (\"Id\" INTEGER NOT NULL PRIMARY KEY, \"Body\" TEXT NOT NULL, \"Subtitle\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"ChnBNotes\" (\"Id\", \"Body\", \"Subtitle\") VALUES (1, 'hello', 'world')");
        db.Execute("CREATE VIRTUAL TABLE \"ChnBNoteSearchOld\" USING fts5(\"Body\", \"Subtitle\", content='ChnBNotes', content_rowid='Id', tokenize='unicode61')");
        db.Execute("CREATE TRIGGER \"ChnBNoteSearchOld_sync_ai\" AFTER INSERT ON \"ChnBNotes\" BEGIN INSERT INTO \"ChnBNoteSearchOld\"(rowid, \"Body\", \"Subtitle\") VALUES (new.\"Id\", new.\"Body\", new.\"Subtitle\"); END");
        db.Execute("CREATE TRIGGER \"ChnBNoteSearchOld_sync_ad\" AFTER DELETE ON \"ChnBNotes\" BEGIN INSERT INTO \"ChnBNoteSearchOld\"(\"ChnBNoteSearchOld\", rowid, \"Body\", \"Subtitle\") VALUES('delete', old.\"Id\", old.\"Body\", old.\"Subtitle\"); END");
        db.Execute("CREATE TRIGGER \"ChnBNoteSearchOld_sync_au\" AFTER UPDATE ON \"ChnBNotes\" BEGIN INSERT INTO \"ChnBNoteSearchOld\"(\"ChnBNoteSearchOld\", rowid, \"Body\", \"Subtitle\") VALUES('delete', old.\"Id\", old.\"Body\", old.\"Subtitle\"); INSERT INTO \"ChnBNoteSearchOld\"(rowid, \"Body\", \"Subtitle\") VALUES (new.\"Id\", new.\"Body\", new.\"Subtitle\"); END");
        db.Execute("INSERT INTO \"ChnBNoteSearchOld\"(rowid, \"Body\", \"Subtitle\") SELECT \"Id\", \"Body\", \"Subtitle\" FROM \"ChnBNotes\"");
    }
}
