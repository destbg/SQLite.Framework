using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecEOwners")]
public class SecEOwner
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("SecENotes")]
public class SecENote
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string Text { get; set; } = "";
}

public class RawSqlDetachRelinkTests
{
    [Fact]
    public void QueryThroughObjectLinkAfterRawSqlDetachThrowsNotSupported()
    {
        using TestDatabase main = new();
        main.Table<SecEOwner>().Schema.CreateTable();
        main.Table<SecEOwner>().Add(new SecEOwner { Id = 1, Name = "owner" });

        string auxPath = AuxPath();
        try
        {
            using SQLiteDatabase aux = OpenAux(auxPath);
            aux.Table<SecENote>().Schema.CreateTable();
            aux.Table<SecENote>().Add(new SecENote { Id = 1, OwnerId = 1, Text = "note" });

            main.AttachDatabase(aux, "SecEAux");
            main.Execute("DETACH DATABASE \"SecEAux\"");

            Assert.Throws<NotSupportedException>(() => (
                from o in main.Table<SecEOwner>()
                join n in aux.Table<SecENote>() on o.Id equals n.OwnerId
                select n.Text
            ).ToList());
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void ReattachedDifferentFileUnderSameSchemaIsNotReadThroughStaleObjectLink()
    {
        using TestDatabase main = new();
        main.Table<SecEOwner>().Schema.CreateTable();
        main.Table<SecEOwner>().Add(new SecEOwner { Id = 1, Name = "owner" });

        string pathA = AuxPath();
        string pathB = AuxPath();
        try
        {
            using SQLiteDatabase auxA = OpenAux(pathA);
            auxA.Table<SecENote>().Schema.CreateTable();
            auxA.Table<SecENote>().Add(new SecENote { Id = 1, OwnerId = 1, Text = "from-a" });

            using SQLiteDatabase auxB = OpenAux(pathB);
            auxB.Table<SecENote>().Schema.CreateTable();
            auxB.Table<SecENote>().Add(new SecENote { Id = 1, OwnerId = 1, Text = "from-b" });

            main.AttachDatabase(auxA, "SecEAux");
            main.Execute("DETACH DATABASE \"SecEAux\"");
            main.AttachDatabase(auxB, "SecEAux");

            Assert.Throws<NotSupportedException>(() => (
                from o in main.Table<SecEOwner>()
                join n in auxA.Table<SecENote>() on o.Id equals n.OwnerId
                select n.Text
            ).ToList());
        }
        finally
        {
            Cleanup(pathA);
            Cleanup(pathB);
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
        return Path.Combine(Path.GetTempPath(), $"sece_{Guid.NewGuid():N}.db3");
    }

    private static void Cleanup(string auxPath)
    {
        if (File.Exists(auxPath))
        {
            File.Delete(auxPath);
        }
    }
}
