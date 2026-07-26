using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22bScopeVaults")]
public class H22bScopeVault
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class AttachedDatabaseObjectScopeTests
{
    [Fact]
    public void RenameTableLeavesTheAttachedDatabaseTableInPlace()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Execute("CREATE TABLE \"H22bScopeVaults\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)");
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h22baux", AuxEncryptionKey);

            Assert.False(main.Schema.TableExists<H22bScopeVault>());

            Exception? failure = Record.Exception(() => main.Schema.RenameTable<H22bScopeVault>("H22bScopeVaultsMoved"));

            Assert.Equal(
                "CREATE TABLE \"H22bScopeVaults\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)",
                main.ExecuteScalar<string>("SELECT sql FROM h22baux.sqlite_master WHERE type = 'table' AND name = 'H22bScopeVaults'"));
            Assert.NotNull(failure);
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void DropViewLeavesTheAttachedDatabaseViewInPlace()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Execute("CREATE TABLE \"H22bScopeVaults\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)");
                auxDb.Execute("CREATE VIEW \"H22bScopeVaultNames\" AS SELECT \"Name\" FROM \"H22bScopeVaults\"");
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h22baux", AuxEncryptionKey);

            Assert.False(main.Schema.ViewExists("H22bScopeVaultNames"));
            main.Schema.DropView("H22bScopeVaultNames");

            Assert.Equal(1, main.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM h22baux.sqlite_master WHERE type = 'view' AND name = 'H22bScopeVaultNames'"));
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    private static string? AuxEncryptionKey =>
#if SQLITECIPHER
        "test-key";
#else
        null;
#endif

    private static string AuxPath()
    {
        return Path.Combine(Path.GetTempPath(), $"h22baux_{Guid.NewGuid():N}.db3");
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
