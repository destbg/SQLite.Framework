using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Models;

namespace SQLite.Framework.Tests;

[Table("H21jCustNote")]
public class H21jCustNote
{
    [Key]
    public int Id { get; set; }

    public required string Body { get; set; }
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(H21jCustNote), AutoSync = FtsAutoSync.Triggers)]
[Table("H21jCustNoteSearch")]
public class H21jCustNoteSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}

public class FtsCustomTriggerNameReplaceSyncTests
{
    [Fact]
    public void AddOrUpdateWithCustomSyncTriggerNamesLeavesNoStaleTerms()
    {
        string path = $"H21jCustSync_{Guid.NewGuid():N}.db3";
        try
        {
            using (SQLiteDatabase db = Open(path))
            {
                db.Table<H21jCustNote>().Schema.CreateTable();
                db.Table<H21jCustNoteSearch>().Schema.CreateTable();
                db.Table<H21jCustNote>().Add(new H21jCustNote { Id = 1, Body = "old apple text" });
            }

            Dictionary<int, string> expected = new() { [1] = "new banana text" };

            using (SQLiteDatabase db = Open(path))
            {
                db.Table<H21jCustNote>().AddOrUpdate(new H21jCustNote { Id = 1, Body = "new banana text" });

                long apple = db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H21jCustNoteSearch\" WHERE \"H21jCustNoteSearch\" MATCH 'apple'");
                long banana = db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H21jCustNoteSearch\" WHERE \"H21jCustNoteSearch\" MATCH 'banana'");

                Assert.Equal(expected.Values.Count(b => b.Contains("apple")), apple);
                Assert.Equal(expected.Values.Count(b => b.Contains("banana")), banana);
            }
        }
        finally
        {
            DeleteFile(path);
        }
    }

    private static SQLiteDatabase Open(string path)
    {
        SQLiteOptionsBuilder builder = new(path);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        builder.UseSchema(database => new H21jNamedTriggerSchema(database));
        return new SQLiteDatabase(builder.Build());
    }

    private static void DeleteFile(string path)
    {
        foreach (string candidate in new[] { path, path + "-wal", path + "-shm" })
        {
            if (File.Exists(candidate))
            {
                File.Delete(candidate);
            }
        }
    }
}

file sealed class H21jNamedTriggerSchema : SQLiteSchema
{
    public H21jNamedTriggerSchema(SQLiteDatabase database)
        : base(database)
    {
    }

    protected override (string ai, string ad, string au) TriggerNamesTuple(TableMapping mapping)
    {
        string baseName = "trg_" + mapping.TableName;
        return (baseName + "_ins", baseName + "_del", baseName + "_upd");
    }
}
