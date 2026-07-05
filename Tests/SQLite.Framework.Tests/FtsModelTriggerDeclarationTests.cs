using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[FullTextSearch]
[Table("NoteSearchFts")]
public class NoteSearchFtsRow
{
    [FullTextIndexed]
    public string Body { get; set; } = "";
}

[Table("FtsAuditNote")]
public class FtsAuditNoteRow
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

file sealed class FtsTriggerModelDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<NoteSearchFtsRow>()
            .Trigger("trg_fts_note", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert,
                t => t.Insert(Table<FtsAuditNoteRow>(), s => s.Set(a => a.Body, _ => t.New.Body)));
    }
}

public class FtsModelTriggerDeclarationTests
{
    [Fact]
    public void DeclaringATriggerOnAnFtsEntityThrows()
    {
        using FtsTriggerModelDb db = new();
        db.Schema.CreateTable<FtsAuditNoteRow>();

        Assert.ThrowsAny<Exception>(() => db.Schema.CreateTable<NoteSearchFtsRow>());
    }
}
