using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecQRenameScope")]
public class SecQRenameScopeRow
{
    [Key]
    public int Id { get; set; }

    public string? NewName { get; set; }
}

[Table("SecQDropScope")]
public class SecQDropScopeRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
}

public class MigrationColumnStepAttachedScopeTests
{
    [Fact]
    public void RenameColumnStepSkipsWhenOnlyAnAttachedDatabaseHasTheTable()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase aux = OpenAux(auxPath))
            {
                aux.Execute("CREATE TABLE \"SecQRenameScope\" (\"Id\" INTEGER PRIMARY KEY, \"OldName\" TEXT)");
                aux.Execute("INSERT INTO \"SecQRenameScope\" VALUES (1, 'keep')");
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "secqauxa", AuxEncryptionKey);

            main.Schema.Migrations()
                .Version(1, m => m.RenameColumn<SecQRenameScopeRow>("OldName", "NewName"))
                .Migrate();

            Assert.False(main.Schema.TableExists<SecQRenameScopeRow>());
            Assert.Equal("keep", main.ExecuteScalar<string>("SELECT \"OldName\" FROM \"secqauxa\".\"SecQRenameScope\" WHERE \"Id\" = 1"));
            Assert.Equal(0L, main.ExecuteScalar<long>("SELECT COUNT(*) FROM pragma_table_xinfo('SecQRenameScope', 'secqauxa') WHERE \"name\" = 'NewName'"));
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void DropColumnStepSkipsWhenOnlyAnAttachedDatabaseHasTheColumn()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase aux = OpenAux(auxPath))
            {
                aux.Execute("CREATE TABLE \"SecQDropScope\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"OldName\" TEXT)");
                aux.Execute("INSERT INTO \"SecQDropScope\" VALUES (1, 'n', 'keep')");
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "secqauxb", AuxEncryptionKey);

            main.Schema.Migrations()
                .Version(1, m => m.DropColumn<SecQDropScopeRow>("OldName"))
                .Migrate();

            Assert.False(main.Schema.TableExists<SecQDropScopeRow>());
            Assert.Equal("keep", main.ExecuteScalar<string>("SELECT \"OldName\" FROM \"secqauxb\".\"SecQDropScope\" WHERE \"Id\" = 1"));
            Assert.Equal(1L, main.ExecuteScalar<long>("SELECT COUNT(*) FROM pragma_table_xinfo('SecQDropScope', 'secqauxb') WHERE \"name\" = 'OldName'"));
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

    private static SQLiteDatabase OpenAux(string path)
    {
        SQLiteOptionsBuilder builder = new(path);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        return new SQLiteDatabase(builder.Build());
    }

    private static string AuxPath()
    {
        return Path.Combine(Path.GetTempPath(), $"secqcol_{Guid.NewGuid():N}.db3");
    }

    private static void Cleanup(string auxPath)
    {
        if (File.Exists(auxPath))
        {
            File.Delete(auxPath);
        }
    }
}
