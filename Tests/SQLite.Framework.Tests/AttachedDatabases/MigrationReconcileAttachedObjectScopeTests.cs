using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26gArchives")]
public class H26gArchive
{
    [Key]
    public int Id { get; set; }

    [Indexed]
    public string Name { get; set; } = "";
}

[Table("H26gArchiveSources")]
public class H26gArchiveSource
{
    [Key]
    public int Id { get; set; }
}

[Table("H26gArchiveAudits")]
public class H26gArchiveAudit
{
    [Key]
    public int Id { get; set; }

    public int SourceId { get; set; }
}

public class H26gArchiveDatabase : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H26gArchiveSource>()
            .Trigger("trgH26gArchiveSync", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert,
                t => t.Insert(Table<H26gArchiveAudit>(), s => s.Set(a => a.SourceId, _ => t.New.Id)));
    }
}

public class MigrationReconcileAttachedObjectScopeTests
{
    [Fact]
    public void ReconcilingTheMainTableLeavesTheAttachedDatabaseIndexInPlace()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase aux = OpenAux(auxPath))
            {
                aux.Execute("CREATE TABLE \"H26gArchives\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
                aux.Execute("CREATE INDEX \"idx_H26gArchives_Name\" ON \"H26gArchives\" (\"Name\")");
            }

            using TestDatabase main = new();
            main.Schema.CreateTable<H26gArchive>();
            main.Schema.DropIndex("idx_H26gArchives_Name");

            main.AttachDatabase(auxPath, "h26gauxa", AuxKey);

            main.Schema.Migrations()
                .Version(1, m => m.TableChanged<H26gArchive>())
                .Migrate();

            Assert.Equal(1, main.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM h26gauxa.sqlite_master WHERE type = 'index' AND name = 'idx_H26gArchives_Name'"));
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void ReconcilingTheMainTableLeavesTheAttachedDatabaseTriggerInPlace()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase aux = OpenAux(auxPath))
            {
                aux.Execute("CREATE TABLE \"H26gArchiveSources\" (\"Id\" INTEGER PRIMARY KEY)");
                aux.Execute("CREATE TABLE \"H26gArchiveAudits\" (\"Id\" INTEGER PRIMARY KEY, \"SourceId\" INTEGER NOT NULL)");
                aux.Execute("CREATE TRIGGER \"trgH26gArchiveSync\" AFTER INSERT ON \"H26gArchiveSources\" BEGIN SELECT 1; END");
            }

            using H26gArchiveDatabase main = new();
            main.Schema.CreateTable<H26gArchiveAudit>();
            main.Schema.CreateTable<H26gArchiveSource>();
            main.Schema.DropTrigger("trgH26gArchiveSync");

            main.AttachDatabase(auxPath, "h26gauxb", AuxKey);

            main.Schema.Migrations()
                .Version(1, m => m.TableChanged<H26gArchiveSource>())
                .Migrate();

            Assert.Equal(1, main.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM h26gauxb.sqlite_master WHERE type = 'trigger' AND name = 'trgH26gArchiveSync'"));
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    private static string? AuxKey =>
#if SQLITECIPHER
        "test-key";
#else
        null;
#endif

    private static string AuxPath()
    {
        return Path.Combine(Path.GetTempPath(), $"h26gaux_{Guid.NewGuid():N}.db3");
    }

    private static SQLiteDatabase OpenAux(string path)
    {
        SQLiteOptionsBuilder builder = new(path);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        return new SQLiteDatabase(builder.Build());
    }

    private static void Cleanup(string auxPath)
    {
        if (File.Exists(auxPath))
        {
            File.Delete(auxPath);
        }
    }
}
