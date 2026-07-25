using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupedConcatCapturedValueTests
{
    private static string Stamp(string value)
    {
        return value + "!";
    }

    [Fact]
    public void CapturedValuesAfterAGroupedJoinKeepTheirSlots()
    {
        using TestDatabase db = new();
        db.Table<KindWordEntryRow>().Schema.CreateTable();
        List<KindWordEntryRow> rows =
        [
            new KindWordEntryRow { Id = 1, Kind = 1, Name = "a" },
            new KindWordEntryRow { Id = 2, Kind = 1, Name = "b" },
            new KindWordEntryRow { Id = 3, Kind = 2, Name = "c" },
        ];
        db.Table<KindWordEntryRow>().AddRange(rows);
        string sep = "|";
        string note = "kept";

        var expected = rows.GroupBy(x => x.Kind)
            .Select(g => new { K = g.Key, S = string.Join(sep, g.Select(x => x.Name)), Note = Stamp(note) })
            .OrderBy(x => x.K).ToList();
        var actual = db.Table<KindWordEntryRow>().GroupBy(x => x.Kind)
            .Select(g => new { K = g.Key, S = string.Join(sep, g.Select(x => x.Name)), Note = Stamp(note) })
            .ToList().OrderBy(x => x.K).ToList();

        Assert.Equal(expected, actual);
    }
}
