using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecGRenamedDocs")]
public class SecGRenamedDoc
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(SecGRenamedDoc), AutoSync = FtsAutoSync.Triggers)]
[Table("SecGDocSearchNew")]
public class SecGDocSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

[Table("SecGOtherDocs")]
public class SecGOtherDoc
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(SecGOtherDoc), AutoSync = FtsAutoSync.Triggers)]
[Table("SecGOtherSearch")]
public class SecGOtherSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class MigrationFtsSearchRenameTests
{
    [Fact]
    public void RenameTableStepKeepsTheSyncTriggersWorking()
    {
        using TestDatabase db = new(useFile: true);
        CreateOldInstall(db);
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SecGRenamedDoc>())
            .Version(2, m => m.RenameTable<SecGDocSearch>("SecGDocSearchOld"))
            .Migrate();

        db.Table<SecGRenamedDoc>().Update(new SecGRenamedDoc { Id = 1, Body = "world" });

        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"SecGDocSearchNew\" WHERE \"SecGDocSearchNew\" MATCH 'hello'"));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"SecGDocSearchNew\" WHERE \"SecGDocSearchNew\" MATCH 'world'"));
    }

    [Fact]
    public void SchemaRenameTableKeepsTheSyncTriggersWorking()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<SecGRenamedDoc>().Schema.CreateTable();
        db.Table<SecGDocSearch>().Schema.CreateTable();
        db.Table<SecGRenamedDoc>().Add(new SecGRenamedDoc { Id = 1, Body = "hello" });

        db.Schema.RenameTable<SecGDocSearch>("SecGDocSearchRenamed");
        db.Table<SecGRenamedDoc>().Update(new SecGRenamedDoc { Id = 1, Body = "world" });

        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"SecGDocSearchRenamed\" WHERE \"SecGDocSearchRenamed\" MATCH 'hello'"));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"SecGDocSearchRenamed\" WHERE \"SecGDocSearchRenamed\" MATCH 'world'"));
    }

    [Fact]
    public void ScriptOfAnFtsRenameContainsNoSavepointBookkeeping()
    {
        using TestDatabase db = new(useFile: true);
        CreateOldInstall(db);
        db.Pragmas.UserVersion = 1;

        IReadOnlyList<string> statements = db.Schema.Migrations()
            .Version(2, m => m.RenameTable<SecGDocSearch>("SecGDocSearchOld"))
            .Script();

        Assert.DoesNotContain(statements, s => s.Contains("SQLITE_AUTOINDEX_", StringComparison.Ordinal));
        Assert.Contains(statements, s => s.Contains("RENAME TO"));
    }

    [Fact]
    public void AFailedRenameLeavesTheSyncTriggersWorking()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<SecGRenamedDoc>().Schema.CreateTable();
        db.Table<SecGDocSearch>().Schema.CreateTable();
        db.Table<SecGRenamedDoc>().Add(new SecGRenamedDoc { Id = 1, Body = "hello" });

        Assert.ThrowsAny<Exception>(() => db.Schema.RenameTable<SecGDocSearch>("SecGRenamedDocs"));
        db.Table<SecGRenamedDoc>().Update(new SecGRenamedDoc { Id = 1, Body = "world" });

        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"SecGDocSearchNew\" WHERE \"SecGDocSearchNew\" MATCH 'hello'"));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"SecGDocSearchNew\" WHERE \"SecGDocSearchNew\" MATCH 'world'"));
    }

    [Fact]
    public void CaseOnlyRenameKeepsTheSyncTriggersWorking()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<SecGRenamedDoc>().Schema.CreateTable();
        db.Table<SecGRenamedDoc>().Add(new SecGRenamedDoc { Id = 1, Body = "hello" });
        db.Execute("CREATE VIRTUAL TABLE \"secgdocsearchnew\" USING fts5(\"Body\", content='SecGRenamedDocs', content_rowid='Id', tokenize='unicode61')");
        db.Execute("CREATE TRIGGER \"secgdocsearchnew_sync_ad\" AFTER DELETE ON \"SecGRenamedDocs\" BEGIN INSERT INTO \"secgdocsearchnew\"(\"secgdocsearchnew\", rowid, \"Body\") VALUES('delete', old.\"Id\", old.\"Body\"); END");
        db.Execute("INSERT INTO \"secgdocsearchnew\"(rowid, \"Body\") SELECT \"Id\", \"Body\" FROM \"SecGRenamedDocs\"");

        db.Schema.Migrations()
            .Version(1, m => m.RenameTable<SecGDocSearch>("secgdocsearchnew"))
            .Migrate();

        db.Table<SecGRenamedDoc>().Remove(new SecGRenamedDoc { Id = 1 });

        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"SecGDocSearchNew\" WHERE \"SecGDocSearchNew\" MATCH 'hello'"));
    }

    [Fact]
    public void ContentRenameRetargetsOnlyTheSearchTableThatReferencesIt()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"SecGDocOld\" (\"Id\" INTEGER NOT NULL PRIMARY KEY, \"Body\" TEXT NOT NULL)");
        db.Execute("CREATE VIRTUAL TABLE \"SecGDocSearchNew\" USING fts5(\"Body\", content='SecGDocOld', content_rowid='Id', tokenize='unicode61')");
        db.Table<SecGOtherDoc>().Schema.CreateTable();
        db.Table<SecGOtherSearch>().Schema.CreateTable();

        db.Schema.Migrations()
            .Version(1, m => m.RenameTable<SecGRenamedDoc>("SecGDocOld"))
            .Migrate();

        string retargeted = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'SecGDocSearchNew'")!;
        string untouched = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'SecGOtherSearch'")!;
        Assert.Contains("SecGRenamedDocs", retargeted);
        Assert.Contains("SecGOtherDocs", untouched);
    }

    private static void CreateOldInstall(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"SecGRenamedDocs\" (\"Id\" INTEGER NOT NULL PRIMARY KEY, \"Body\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"SecGRenamedDocs\" (\"Id\", \"Body\") VALUES (1, 'hello')");
        db.Execute("CREATE VIRTUAL TABLE \"SecGDocSearchOld\" USING fts5(\"Body\", content='SecGRenamedDocs', content_rowid='Id', tokenize='unicode61')");
        db.Execute("CREATE TRIGGER \"SecGDocSearchOld_sync_ai\" AFTER INSERT ON \"SecGRenamedDocs\" BEGIN INSERT INTO \"SecGDocSearchOld\"(rowid, \"Body\") VALUES (new.\"Id\", new.\"Body\"); END");
        db.Execute("CREATE TRIGGER \"SecGDocSearchOld_sync_ad\" AFTER DELETE ON \"SecGRenamedDocs\" BEGIN INSERT INTO \"SecGDocSearchOld\"(\"SecGDocSearchOld\", rowid, \"Body\") VALUES('delete', old.\"Id\", old.\"Body\"); END");
        db.Execute("CREATE TRIGGER \"SecGDocSearchOld_sync_au\" AFTER UPDATE ON \"SecGRenamedDocs\" BEGIN INSERT INTO \"SecGDocSearchOld\"(\"SecGDocSearchOld\", rowid, \"Body\") VALUES('delete', old.\"Id\", old.\"Body\"); INSERT INTO \"SecGDocSearchOld\"(rowid, \"Body\") VALUES (new.\"Id\", new.\"Body\"); END");
        db.Execute("INSERT INTO \"SecGDocSearchOld\"(rowid, \"Body\") SELECT \"Id\", \"Body\" FROM \"SecGRenamedDocs\"");
    }
}
