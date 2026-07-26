using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22bAuxPlains")]
public class H22bAuxPlain
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

[WithoutRowId]
[Table("H22bAuxKeyed")]
public class H22bAuxKeyed
{
    [Key]
    public required string Code { get; set; }

    public int Value { get; set; }
}

public class ValidateModelAttachedTableScopeTests
{
    [Fact]
    public void ValidateModelJudgesAttachedOnlyTablesTheSameWhateverTheTableOptionsAre()
    {
        string auxPath = AuxPath();
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Execute("CREATE TABLE \"H22bAuxPlains\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
                auxDb.Execute("CREATE TABLE \"H22bAuxKeyed\" (\"Code\" TEXT NOT NULL PRIMARY KEY, \"Value\" INTEGER NOT NULL) WITHOUT ROWID");
            }

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h22baux2", AuxEncryptionKey);

            Assert.False(main.Schema.TableExists<H22bAuxPlain>());
            Assert.False(main.Schema.TableExists<H22bAuxKeyed>());

            SQLiteModelValidationResult plain = main.Schema.ValidateModel<H22bAuxPlain>();
            SQLiteModelValidationResult keyed = main.Schema.ValidateModel<H22bAuxKeyed>();

            Assert.True(
                plain.IsValid == keyed.IsValid,
                string.Join(" | ", plain.Issues) + " / " + string.Join(" | ", keyed.Issues));
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
        return Path.Combine(Path.GetTempPath(), $"h22baux2_{Guid.NewGuid():N}.db3");
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
