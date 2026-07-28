using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23kTrackRows")]
public class H23kTrackRow
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public int Length { get; set; }

    public int? Slot { get; set; }

    public int Value { get; set; }
}

public class ScalarGroupKeyMemberResolutionTests
{
    [Fact]
    public void StringKeyLengthReadsTheGroupKey()
    {
        using TestDatabase db = Setup(nameof(StringKeyLengthReadsTheGroupKey));

        List<(string Title, int Chars)> expected = Rows()
            .GroupBy(r => r.Title)
            .Select(g => new { Title = g.Key, Chars = g.Key.Length })
            .OrderBy(x => x.Title, StringComparer.Ordinal)
            .Select(x => (x.Title, x.Chars))
            .ToList();

        List<(string Title, int Chars)> actual = db.Table<H23kTrackRow>()
            .GroupBy(r => r.Title)
            .Select(g => new { Title = g.Key, Chars = g.Key.Length })
            .ToList()
            .OrderBy(x => x.Title, StringComparer.Ordinal)
            .Select(x => (x.Title, x.Chars))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableKeyValueReadsTheGroupKey()
    {
        using TestDatabase db = Setup(nameof(NullableKeyValueReadsTheGroupKey));

        List<(int? Slot, int Read)> expected = Rows()
            .GroupBy(r => r.Slot)
            .Select(g => new { Slot = g.Key, Read = g.Key!.Value })
            .OrderBy(x => x.Slot)
            .Select(x => (x.Slot, x.Read))
            .ToList();

        List<(int? Slot, int Read)> actual = db.Table<H23kTrackRow>()
            .GroupBy(r => r.Slot)
            .Select(g => new { Slot = g.Key, Read = g.Key!.Value })
            .ToList()
            .OrderBy(x => x.Slot)
            .Select(x => (x.Slot, x.Read))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23kTrackRow> Rows()
    {
        return
        [
            new H23kTrackRow { Id = 1, Title = "aa", Length = 300, Slot = 5, Value = 11 },
            new H23kTrackRow { Id = 2, Title = "aa", Length = 250, Slot = 5, Value = 12 },
            new H23kTrackRow { Id = 3, Title = "bbbb", Length = 180, Slot = 7, Value = 13 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23kTrackRow>().Schema.CreateTable();
        db.Table<H23kTrackRow>().AddRange(Rows());
        return db;
    }
}
