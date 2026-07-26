using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22cScopeVault")]
public class H22cScopeVault
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H22cScopeVaultView")]
public class H22cScopeVaultView
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class AttachedDatabaseViewScopeTests
{
    private static string? AuxEncryptionKey =>
#if SQLITECIPHER
        "test-key";
#else
        null;
#endif

    [Fact]
    public void DropViewByNameLeavesTheAttachedDatabaseViewInPlace()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                CreateAuxObjects(auxDb);
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h22caux", AuxEncryptionKey);

            Assert.False(main.Schema.ViewExists("H22cScopeVaultView"));
            main.Schema.DropView("H22cScopeVaultView");

            Assert.Equal(1, main.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM h22caux.sqlite_master WHERE type = 'view' AND name = 'H22cScopeVaultView'"));
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void DropViewForAnEntityLeavesTheAttachedDatabaseViewInPlace()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                CreateAuxObjects(auxDb);
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h22caux", AuxEncryptionKey);

            Assert.False(main.Schema.ViewExists<H22cScopeVaultView>());
            main.Schema.DropView<H22cScopeVaultView>();

            Assert.Equal(1, main.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM h22caux.sqlite_master WHERE type = 'view' AND name = 'H22cScopeVaultView'"));
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void DropViewLeavesTheAttachedDatabaseViewRowsReadable()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                CreateAuxObjects(auxDb);
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h22caux", AuxEncryptionKey);
            main.Schema.DropView("H22cScopeVaultView");

            List<string> expected = Vaults().OrderBy(v => v.Id).Select(v => v.Name).ToList();
            List<string> actual = main.Table<H22cScopeVaultView>("h22caux")
                .OrderBy(v => v.Id)
                .Select(v => v.Name)
                .ToList();

            Assert.Equal(expected, actual);
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    private static List<H22cScopeVault> Vaults()
    {
        return
        [
            new H22cScopeVault { Id = 1, Name = "alpha" },
            new H22cScopeVault { Id = 2, Name = "beta" }
        ];
    }

    private static void CreateAuxObjects(SQLiteDatabase auxDb)
    {
        auxDb.Table<H22cScopeVault>().Schema.CreateTable();
        auxDb.Table<H22cScopeVault>().AddRange(Vaults());
        auxDb.Execute("CREATE VIEW \"H22cScopeVaultView\" AS SELECT \"Id\", \"Name\" FROM \"H22cScopeVault\"");
    }

    private static string AuxPath()
    {
        return Path.Combine(Path.GetTempPath(), $"h22caux_{Guid.NewGuid():N}.db3");
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
