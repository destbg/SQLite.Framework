using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25kCaseOwners")]
public class H25kCaseOwner
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H25kCaseNotes")]
public class H25kCaseNote
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string Text { get; set; } = "";
}

public class DetachedSchemaNameCasingTests
{
    [Fact]
    public void DetachingWithADifferentSchemaNameCasingClearsThePrefixLookup()
    {
        using TestDatabase main = new();
        main.Table<H25kCaseOwner>().Schema.CreateTable();
        main.Table<H25kCaseOwner>().Add(new H25kCaseOwner { Id = 1, Name = "owner" });

        string auxPath = AuxPath();
        try
        {
            using SQLiteDatabase aux = OpenAux(auxPath);
            aux.Table<H25kCaseNote>().Schema.CreateTable();
            aux.Table<H25kCaseNote>().Add(new H25kCaseNote { Id = 1, OwnerId = 1, Text = "note" });

            main.AttachDatabase(aux, "H25kCaseAux");

            List<string> before = (
                from o in main.Table<H25kCaseOwner>()
                join n in aux.Table<H25kCaseNote>() on o.Id equals n.OwnerId
                select n.Text
            ).ToList();
            Assert.Equal(["note"], before);

            main.DetachDatabase("h25kcaseaux");

            Assert.Throws<NotSupportedException>(() => (
                from o in main.Table<H25kCaseOwner>()
                join n in aux.Table<H25kCaseNote>() on o.Id equals n.OwnerId
                select n.Text
            ).ToList());
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

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
        return Path.Combine(Path.GetTempPath(), $"h25kcase_{Guid.NewGuid():N}.db3");
    }

    private static void Cleanup(string auxPath)
    {
        if (File.Exists(auxPath))
        {
            File.Delete(auxPath);
        }
    }
}
