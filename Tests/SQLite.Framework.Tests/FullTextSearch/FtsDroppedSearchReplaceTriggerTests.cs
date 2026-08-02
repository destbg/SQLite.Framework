using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecGDroppedNotes")]
public class SecGDroppedNote
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(SecGDroppedNote), AutoSync = FtsAutoSync.Triggers)]
[Table("SecGDroppedNoteSearch")]
public class SecGDroppedNoteSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class FtsDroppedSearchReplaceTriggerTests
{
    [Fact]
    public void ReplaceWriteAfterSearchTableDropDoesNotFireDeleteTriggers()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<SecGDroppedNote>().Schema.CreateTable();
        db.Table<SecGDroppedNoteSearch>().Schema.CreateTable();
        db.Execute("CREATE TABLE \"SecGDroppedNoteAudit\" (\"Body\" TEXT NOT NULL)");
        db.Execute("CREATE TRIGGER \"SecGDroppedNoteAuditTrigger\" AFTER DELETE ON \"SecGDroppedNotes\" BEGIN INSERT INTO \"SecGDroppedNoteAudit\" (\"Body\") VALUES (old.\"Body\"); END");
        db.Table<SecGDroppedNote>().Add(new SecGDroppedNote { Id = 1, Body = "one" });

        db.Schema.DropTable<SecGDroppedNoteSearch>();
        db.Table<SecGDroppedNote>().AddOrUpdate(new SecGDroppedNote { Id = 1, Body = "two" });

        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"SecGDroppedNoteAudit\""));
        Assert.Equal("two", db.Table<SecGDroppedNote>().Single().Body);
    }
}
