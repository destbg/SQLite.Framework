using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20DbSyncNote")]
public class H20DbSyncNote
{
    [Key]
    public int Id { get; set; }
    public required string Body { get; set; }
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(H20DbSyncNote), AutoSync = FtsAutoSync.Triggers)]
[Table("H20DbSyncNoteSearch")]
public class H20DbSyncNoteSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}

public class FtsUnmappedSessionReplaceSyncTests
{
    [Fact]
    public void AddOrUpdateInSessionWithoutFtsMappingLeavesNoGhostTerms()
    {
        string path = $"H20DbSyncSingle_{Guid.NewGuid():N}.db3";
        try
        {
            using (SQLiteDatabase db = OpenRaw(path))
            {
                db.Table<H20DbSyncNote>().Schema.CreateTable();
                db.Table<H20DbSyncNoteSearch>().Schema.CreateTable();
                db.Table<H20DbSyncNote>().Add(new H20DbSyncNote { Id = 1, Body = "old apple text" });
            }

            Dictionary<int, string> expected = new() { [1] = "new banana text" };

            using (SQLiteDatabase db = OpenRaw(path))
            {
                db.Table<H20DbSyncNote>().AddOrUpdate(new H20DbSyncNote { Id = 1, Body = "new banana text" });

                long apple = db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H20DbSyncNoteSearch\" WHERE \"H20DbSyncNoteSearch\" MATCH 'apple'");
                long banana = db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H20DbSyncNoteSearch\" WHERE \"H20DbSyncNoteSearch\" MATCH 'banana'");

                Assert.Equal(expected.Values.Count(b => b.Contains("apple")), apple);
                Assert.Equal(expected.Values.Count(b => b.Contains("banana")), banana);
            }
        }
        finally
        {
            DeleteFile(path);
        }
    }

    [Fact]
    public void AddOrUpdateRangeInSessionWithoutFtsMappingLeavesNoGhostTerms()
    {
        string path = $"H20DbSyncRange_{Guid.NewGuid():N}.db3";
        try
        {
            using (SQLiteDatabase db = OpenRaw(path))
            {
                db.Table<H20DbSyncNote>().Schema.CreateTable();
                db.Table<H20DbSyncNoteSearch>().Schema.CreateTable();
                db.Table<H20DbSyncNote>().AddRange(new[]
                {
                    new H20DbSyncNote { Id = 1, Body = "old apple text" },
                    new H20DbSyncNote { Id = 2, Body = "plain filler text" },
                });
            }

            Dictionary<int, string> expected = new()
            {
                [1] = "new banana text",
                [2] = "plain filler text",
            };

            using (SQLiteDatabase db = OpenRaw(path))
            {
                db.Table<H20DbSyncNote>().AddOrUpdateRange(new[]
                {
                    new H20DbSyncNote { Id = 1, Body = "new banana text" },
                    new H20DbSyncNote { Id = 2, Body = "plain filler text" },
                });

                long apple = db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H20DbSyncNoteSearch\" WHERE \"H20DbSyncNoteSearch\" MATCH 'apple'");
                long banana = db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H20DbSyncNoteSearch\" WHERE \"H20DbSyncNoteSearch\" MATCH 'banana'");

                Assert.Equal(expected.Values.Count(b => b.Contains("apple")), apple);
                Assert.Equal(expected.Values.Count(b => b.Contains("banana")), banana);
            }
        }
        finally
        {
            DeleteFile(path);
        }
    }

    private static SQLiteDatabase OpenRaw(string path)
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
