using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21lVaults")]
public class H21lVault
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H21lVaultsAlt")]
public class H21lVaultAlt
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Extra { get; set; }
}

public class AttachedDatabaseSchemaScopeTests
{
    [Fact]
    public void RenameColumnLeavesTheAttachedDatabaseSchemaUnchanged()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Execute("CREATE TABLE \"H21lVaultsAlt\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)");
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h21laux", AuxEncryptionKey);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => main.Schema.RenameColumn<H21lVaultAlt>("Name", "Label"));
            Assert.Equal(
                "Table 'H21lVaultsAlt' for entity 'H21lVaultAlt' does not exist in the main database, so its columns cannot be changed. Create the table with CreateTable first.",
                exception.Message);

            Assert.Equal(
                "CREATE TABLE \"H21lVaultsAlt\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)",
                main.ExecuteScalar<string>("SELECT sql FROM h21laux.sqlite_master WHERE name = 'H21lVaultsAlt'"));
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void DropColumnLeavesTheAttachedDatabaseSchemaUnchanged()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Execute("CREATE TABLE \"H21lVaultsAlt\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)");
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h21laux", AuxEncryptionKey);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => main.Schema.DropColumn<H21lVaultAlt>("Name"));
            Assert.Equal(
                "Table 'H21lVaultsAlt' for entity 'H21lVaultAlt' does not exist in the main database, so its columns cannot be changed. Create the table with CreateTable first.",
                exception.Message);

            Assert.Equal(
                "CREATE TABLE \"H21lVaultsAlt\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)",
                main.ExecuteScalar<string>("SELECT sql FROM h21laux.sqlite_master WHERE name = 'H21lVaultsAlt'"));
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void AddColumnOnAMainTableStillWorks()
    {
        using TestDatabase main = new();
        main.Execute("CREATE TABLE \"H21lVaultsAlt\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)");

        main.Schema.AddColumn<H21lVaultAlt>("Extra");

        Assert.True(main.Schema.ColumnExists<H21lVaultAlt>("Extra"));
    }

    [Fact]
    public void DropTableLeavesTheAttachedDatabaseTableInPlace()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Table<H21lVault>().Schema.CreateTable();
                auxDb.Table<H21lVault>().AddRange(Vaults());
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h21laux", AuxEncryptionKey);

            Assert.False(main.Schema.TableExists<H21lVault>());
            main.Schema.DropTable<H21lVault>();

            Assert.Equal(1, main.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM h21laux.sqlite_master WHERE type = 'table' AND name = 'H21lVaults'"));
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void DropTableLeavesTheAttachedDatabaseRowsReadable()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Table<H21lVault>().Schema.CreateTable();
                auxDb.Table<H21lVault>().AddRange(Vaults());
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h21laux", AuxEncryptionKey);
            main.Schema.DropTable<H21lVault>();

            List<string> expected = Vaults().OrderBy(v => v.Id).Select(v => v.Name).ToList();
            List<string> actual = main.Table<H21lVault>("h21laux").OrderBy(v => v.Id).Select(v => v.Name).ToList();

            Assert.Equal(expected, actual);
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void ColumnExistsIsFalseWhenOnlyTheAttachedDatabaseHasTheTable()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Table<H21lVault>().Schema.CreateTable();
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h21laux", AuxEncryptionKey);

            Assert.False(main.Schema.TableExists<H21lVault>());
            Assert.False(main.Schema.ColumnExists<H21lVault>("Name"));
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void ListColumnsIsEmptyWhenOnlyTheAttachedDatabaseHasTheTable()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Table<H21lVault>().Schema.CreateTable();
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h21laux", AuxEncryptionKey);

            IReadOnlyList<SchemaColumnInfo> columns = main.Schema.ListColumns<H21lVault>();

            Assert.Empty(columns);
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void AddColumnLeavesTheAttachedDatabaseSchemaUnchanged()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Execute("CREATE TABLE \"H21lVaultsAlt\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)");
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h21laux", AuxEncryptionKey);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => main.Schema.AddColumn<H21lVaultAlt>("Extra"));
            Assert.Equal(
                "Table 'H21lVaultsAlt' for entity 'H21lVaultAlt' does not exist in the main database, so its columns cannot be changed. Create the table with CreateTable first.",
                exception.Message);

            Assert.Equal(
                "CREATE TABLE \"H21lVaultsAlt\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)",
                main.ExecuteScalar<string>("SELECT sql FROM h21laux.sqlite_master WHERE name = 'H21lVaultsAlt'"));
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void DropIndexLeavesTheAttachedDatabaseIndexInPlace()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Table<H21lVault>().Schema.CreateTable();
                auxDb.Schema.CreateIndex<H21lVault>(v => v.Name, "idxH21lVaultName");
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h21laux", AuxEncryptionKey);

            Assert.False(main.Schema.IndexExists("idxH21lVaultName"));
            main.Schema.DropIndex("idxH21lVaultName");

            Assert.Equal(1, main.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM h21laux.sqlite_master WHERE type = 'index' AND name = 'idxH21lVaultName'"));
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

    private static List<H21lVault> Vaults()
    {
        return
        [
            new H21lVault { Id = 1, Name = "alpha" },
            new H21lVault { Id = 2, Name = "beta" }
        ];
    }

    private static string AuxPath()
    {
        return Path.Combine(Path.GetTempPath(), $"h21laux_{Guid.NewGuid():N}.db3");
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
