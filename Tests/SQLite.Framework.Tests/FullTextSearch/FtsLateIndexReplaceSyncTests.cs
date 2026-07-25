using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests;

[Table("H21jLateNote")]
public class H21jLateNote
{
    [Key]
    public int Id { get; set; }

    public required string Body { get; set; }
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(H21jLateNote), AutoSync = FtsAutoSync.Triggers)]
[Table("H21jLateNoteSearch")]
public class H21jLateNoteSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}

public class FtsLateIndexReplaceSyncTests
{
    [Fact]
    public void AddOrUpdateAfterTheIndexAppearsLeavesNoStaleTerms()
    {
        string path = $"H21jLateSingle_{Guid.NewGuid():N}.db3";
        try
        {
            using SQLiteDatabase writer = Open(path);
            writer.Table<H21jLateNote>().Schema.CreateTable();
            writer.Table<H21jLateNote>().AddOrUpdate(new H21jLateNote { Id = 1, Body = "old apple text" });

            using (SQLiteDatabase setup = Open(path))
            {
                setup.Table<H21jLateNoteSearch>().Schema.CreateTable();
                setup.Execute("INSERT INTO \"H21jLateNoteSearch\"(\"H21jLateNoteSearch\") VALUES('rebuild')");
            }

            Dictionary<int, string> expected = new() { [1] = "new banana text" };
            writer.Table<H21jLateNote>().AddOrUpdate(new H21jLateNote { Id = 1, Body = "new banana text" });

            long apple = writer.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H21jLateNoteSearch\" WHERE \"H21jLateNoteSearch\" MATCH 'apple'");
            long banana = writer.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H21jLateNoteSearch\" WHERE \"H21jLateNoteSearch\" MATCH 'banana'");

            Assert.Equal(expected.Values.Count(b => b.Contains("apple")), apple);
            Assert.Equal(expected.Values.Count(b => b.Contains("banana")), banana);
        }
        finally
        {
            DeleteFile(path);
        }
    }

    [Fact]
    public void AddOrUpdateRangeAfterTheIndexAppearsLeavesNoStaleTerms()
    {
        string path = $"H21jLateRange_{Guid.NewGuid():N}.db3";
        try
        {
            using SQLiteDatabase writer = Open(path);
            writer.Table<H21jLateNote>().Schema.CreateTable();
            writer.Table<H21jLateNote>().AddOrUpdateRange(new[]
            {
                new H21jLateNote { Id = 1, Body = "old apple text" },
                new H21jLateNote { Id = 2, Body = "plain filler text" },
            });

            using (SQLiteDatabase setup = Open(path))
            {
                setup.Table<H21jLateNoteSearch>().Schema.CreateTable();
                setup.Execute("INSERT INTO \"H21jLateNoteSearch\"(\"H21jLateNoteSearch\") VALUES('rebuild')");
            }

            Dictionary<int, string> expected = new()
            {
                [1] = "new banana text",
                [2] = "plain filler text",
            };
            writer.Table<H21jLateNote>().AddOrUpdateRange(new[]
            {
                new H21jLateNote { Id = 1, Body = "new banana text" },
                new H21jLateNote { Id = 2, Body = "plain filler text" },
            });

            long apple = writer.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H21jLateNoteSearch\" WHERE \"H21jLateNoteSearch\" MATCH 'apple'");
            long banana = writer.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H21jLateNoteSearch\" WHERE \"H21jLateNoteSearch\" MATCH 'banana'");

            Assert.Equal(expected.Values.Count(b => b.Contains("apple")), apple);
            Assert.Equal(expected.Values.Count(b => b.Contains("banana")), banana);
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
