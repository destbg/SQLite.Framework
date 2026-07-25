using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FkTxParentRows")]
public class UserTransactionFkParentRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Extra { get; set; }
}

[Table("FkTxChildRows")]
public class UserTransactionFkChildRow
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(UserTransactionFkParentRow))]
    public int ParentId { get; set; }
}

public class MigrationUserTransactionForeignKeyEnforcementTests
{
    private static void Setup(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"FkTxParentRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("CREATE TABLE \"FkTxChildRows\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER NOT NULL, FOREIGN KEY (\"ParentId\") REFERENCES \"FkTxParentRows\" (\"Id\"))");
        db.Execute("INSERT INTO \"FkTxParentRows\" (\"Id\", \"Name\") VALUES (1, 'p1')");
        db.Execute("INSERT INTO \"FkTxChildRows\" (\"Id\", \"ParentId\") VALUES (1, 1)");
    }

    private static SQLiteMigrationRunner RebuildChain(TestDatabase db)
    {
        return db.Schema.Migrations()
            .Version(1, m => m.TableChanged<UserTransactionFkParentRow>(rebuild: true));
    }

    [Fact]
    public void ViolatingInsertAfterMigrateInsideUserTransactionThrowsImmediately()
    {
        using TestDatabase db = new(useFile: true);
        Setup(db);

        using SQLiteTransaction tx = db.BeginTransaction();
        RebuildChain(db).Migrate();

        Assert.Throws<SQLiteException>(() =>
            db.Execute("INSERT INTO \"FkTxChildRows\" (\"Id\", \"ParentId\") VALUES (99, 12345)"));
    }

    [Fact]
    public async Task ViolatingInsertAfterMigrateAsyncInsideUserTransactionThrowsImmediately()
    {
        using TestDatabase db = new(useFile: true);
        Setup(db);

        using SQLiteTransaction tx = db.BeginTransaction();
        await RebuildChain(db).MigrateAsync();

        Assert.Throws<SQLiteException>(() =>
            db.Execute("INSERT INTO \"FkTxChildRows\" (\"Id\", \"ParentId\") VALUES (99, 12345)"));
    }

    [Fact]
    public void ViolatingInsertAfterScriptInsideUserTransactionThrowsImmediately()
    {
        using TestDatabase db = new(useFile: true);
        Setup(db);

        using SQLiteTransaction tx = db.BeginTransaction();
        RebuildChain(db).Script();

        Assert.Throws<SQLiteException>(() =>
            db.Execute("INSERT INTO \"FkTxChildRows\" (\"Id\", \"ParentId\") VALUES (99, 12345)"));
    }

    [Fact]
    public void DeferredForeignKeyPragmaIsOffAfterMigrateInsideUserTransaction()
    {
        using TestDatabase db = new(useFile: true);
        Setup(db);

        using SQLiteTransaction tx = db.BeginTransaction();
        RebuildChain(db).Migrate();

        long foreignKeys = db.ExecuteScalar<long>("PRAGMA foreign_keys");
        long deferred = db.ExecuteScalar<long>("PRAGMA defer_foreign_keys");
        Assert.Equal("foreign_keys=1 defer_foreign_keys=0", $"foreign_keys={foreignKeys} defer_foreign_keys={deferred}");
    }
}
