using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecERelOwners")]
public class SecERelOwner
{
    [Key]
    public int Id { get; set; }
}

[Table("SecERelNotes")]
public class SecERelNote
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string Text { get; set; } = "";
}

public class AttachedDatabaseRelativePathTests
{
    [Fact]
    public void ObjectLinkResolvesThroughARelativePathAttach()
    {
        string fileName = $"sece_rel_{Guid.NewGuid():N}.db3";
        string relativePath = Path.Combine(".", fileName);
        try
        {
            using TestDatabase main = new();
            main.Table<SecERelOwner>().Schema.CreateTable();
            main.Table<SecERelOwner>().Add(new SecERelOwner { Id = 1 });

            SQLiteOptionsBuilder auxBuilder = new(relativePath);
#if SQLITECIPHER
            auxBuilder.UseEncryptionKey("test-key");
#endif
            using SQLiteDatabase aux = new(auxBuilder.Build());
            aux.Table<SecERelNote>().Schema.CreateTable();
            aux.Table<SecERelNote>().Add(new SecERelNote { Id = 1, OwnerId = 1, Text = "rel" });

            main.AttachDatabase(aux, "SecERelAux");

            string text = (
                from o in main.Table<SecERelOwner>()
                join n in aux.Table<SecERelNote>() on o.Id equals n.OwnerId
                select n.Text
            ).Single();
            Assert.Equal("rel", text);
        }
        finally
        {
            if (File.Exists(relativePath))
            {
                File.Delete(relativePath);
            }
        }
    }
}
