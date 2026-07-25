using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("LftsSyncNote")]
public class LftsSyncNote
{
    [Key]
    public int Id { get; set; }
    public required string Body { get; set; }
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(LftsSyncNote), AutoSync = FtsAutoSync.Triggers)]
[Table("LftsSyncNoteSearch")]
public class LftsSyncNoteSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}

public class FtsExternalContentConflictSyncTests
{
    [Fact]
    public void AddOrUpdateConflictRemovesOldTermsFromIndex()
    {
        using TestDatabase db = new();
        db.Table<LftsSyncNote>().Schema.CreateTable();
        db.Table<LftsSyncNoteSearch>().Schema.CreateTable();

        db.Table<LftsSyncNote>().Add(new LftsSyncNote { Id = 1, Body = "old apple text" });
        db.Table<LftsSyncNote>().AddOrUpdate(new LftsSyncNote { Id = 1, Body = "new banana text" });

        Dictionary<int, string> oracle = new();
        oracle[1] = "old apple text";
        oracle[1] = "new banana text";

        List<LftsSyncNoteSearch> apple = db.Table<LftsSyncNoteSearch>()
            .Where(s => SQLiteFTS5Functions.Match(s, "apple"))
            .ToList();
        List<LftsSyncNoteSearch> banana = db.Table<LftsSyncNoteSearch>()
            .Where(s => SQLiteFTS5Functions.Match(s, "banana"))
            .ToList();

        Assert.Equal(oracle.Values.Count(b => b.Contains("apple")), apple.Count);
        Assert.Equal(oracle.Values.Count(b => b.Contains("banana")), banana.Count);
    }

    [Fact]
    public void AddOrUpdateRangeConflictRemovesOldTermsFromIndex()
    {
        using TestDatabase db = new();
        db.Table<LftsSyncNote>().Schema.CreateTable();
        db.Table<LftsSyncNoteSearch>().Schema.CreateTable();

        db.Table<LftsSyncNote>().AddRange(new[]
        {
            new LftsSyncNote { Id = 1, Body = "old apple text" },
            new LftsSyncNote { Id = 2, Body = "plain filler text" },
        });
        db.Table<LftsSyncNote>().AddOrUpdateRange(new[]
        {
            new LftsSyncNote { Id = 1, Body = "new banana text" },
            new LftsSyncNote { Id = 3, Body = "another cherry note" },
        });

        Dictionary<int, string> oracle = new();
        oracle[1] = "old apple text";
        oracle[2] = "plain filler text";
        oracle[1] = "new banana text";
        oracle[3] = "another cherry note";

        List<LftsSyncNoteSearch> apple = db.Table<LftsSyncNoteSearch>()
            .Where(s => SQLiteFTS5Functions.Match(s, "apple"))
            .ToList();
        List<LftsSyncNoteSearch> banana = db.Table<LftsSyncNoteSearch>()
            .Where(s => SQLiteFTS5Functions.Match(s, "banana"))
            .ToList();
        List<LftsSyncNoteSearch> cherry = db.Table<LftsSyncNoteSearch>()
            .Where(s => SQLiteFTS5Functions.Match(s, "cherry"))
            .ToList();

        Assert.Equal(oracle.Values.Count(b => b.Contains("apple")), apple.Count);
        Assert.Equal(oracle.Values.Count(b => b.Contains("banana")), banana.Count);
        Assert.Equal(oracle.Values.Count(b => b.Contains("cherry")), cherry.Count);
    }
}
