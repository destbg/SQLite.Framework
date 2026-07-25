using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;

namespace SQLite.Framework.Tests;

[Table("H20WrtNote")]
public class H20WrtNote
{
    [Key]
    public int Id { get; set; }
    public required string Body { get; set; }
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(H20WrtNote), AutoSync = FtsAutoSync.Triggers)]
[Table("H20WrtNoteSearch")]
public class H20WrtNoteSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}

public class FtsReopenedIndexReplaceSyncTests
{
    [Fact]
    public void AddOrUpdateAfterReopenRemovesOldTermsFromIndex()
    {
        string path = TempPath();
        try
        {
            using (SQLiteDatabase db = Open(path))
            {
                db.Table<H20WrtNote>().Schema.CreateTable();
                db.Table<H20WrtNoteSearch>().Schema.CreateTable();
                db.Table<H20WrtNote>().Add(new H20WrtNote { Id = 1, Body = "old apple text" });
                db.Table<H20WrtNote>().Add(new H20WrtNote { Id = 2, Body = "plain filler text" });
            }

            Dictionary<int, string> expected = new()
            {
                [1] = "old apple text",
                [2] = "plain filler text",
            };

            using (SQLiteDatabase db = Open(path))
            {
                db.Table<H20WrtNote>().AddOrUpdate(new H20WrtNote { Id = 1, Body = "new banana text" });
                expected[1] = "new banana text";

                List<H20WrtNoteSearch> apple = db.Table<H20WrtNoteSearch>()
                    .Where(s => SQLiteFTS5Functions.Match(s, "apple"))
                    .ToList();
                List<H20WrtNoteSearch> banana = db.Table<H20WrtNoteSearch>()
                    .Where(s => SQLiteFTS5Functions.Match(s, "banana"))
                    .ToList();

                Assert.Equal(expected.Values.Count(b => b.Contains("apple")), apple.Count);
                Assert.Equal(expected.Values.Count(b => b.Contains("banana")), banana.Count);
            }
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public void AddOrUpdateRangeAfterReopenRemovesOldTermsFromIndex()
    {
        string path = TempPath();
        try
        {
            using (SQLiteDatabase db = Open(path))
            {
                db.Table<H20WrtNote>().Schema.CreateTable();
                db.Table<H20WrtNoteSearch>().Schema.CreateTable();
                db.Table<H20WrtNote>().AddRange(new[]
                {
                    new H20WrtNote { Id = 1, Body = "old apple text" },
                    new H20WrtNote { Id = 2, Body = "plain filler text" },
                });
            }

            Dictionary<int, string> expected = new()
            {
                [1] = "old apple text",
                [2] = "plain filler text",
            };

            using (SQLiteDatabase db = Open(path))
            {
                db.Table<H20WrtNote>().AddOrUpdateRange(new[]
                {
                    new H20WrtNote { Id = 1, Body = "new banana text" },
                    new H20WrtNote { Id = 3, Body = "another cherry note" },
                });
                expected[1] = "new banana text";
                expected[3] = "another cherry note";

                List<H20WrtNoteSearch> apple = db.Table<H20WrtNoteSearch>()
                    .Where(s => SQLiteFTS5Functions.Match(s, "apple"))
                    .ToList();
                List<H20WrtNoteSearch> cherry = db.Table<H20WrtNoteSearch>()
                    .Where(s => SQLiteFTS5Functions.Match(s, "cherry"))
                    .ToList();

                Assert.Equal(expected.Values.Count(b => b.Contains("apple")), apple.Count);
                Assert.Equal(expected.Values.Count(b => b.Contains("cherry")), cherry.Count);
            }
        }
        finally
        {
            Delete(path);
        }
    }

    private static string TempPath()
    {
        return Path.Combine(Path.GetTempPath(), $"h20wrt_{Guid.NewGuid():N}.db3");
    }

    private static void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
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
}
