using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupedConcatSeparatorShapeTests
{
    private static TestDatabase Seed(out List<KindWordEntryRow> rows, string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<KindWordEntryRow>().Schema.CreateTable();
        rows =
        [
            new KindWordEntryRow { Id = 1, Kind = 1, Name = "a" },
            new KindWordEntryRow { Id = 2, Kind = 1, Name = "a" },
            new KindWordEntryRow { Id = 3, Kind = 1, Name = "b" },
            new KindWordEntryRow { Id = 4, Kind = 2, Name = "c" },
        ];
        db.Table<KindWordEntryRow>().AddRange(rows);
        return db;
    }

    [Fact]
    public void DistinctJoinWithACharCommaSeparatorTranslates()
    {
        using TestDatabase db = Seed(out List<KindWordEntryRow> rows, nameof(DistinctJoinWithACharCommaSeparatorTranslates));

        var expected = rows.GroupBy(x => x.Kind)
            .Select(g => new { K = g.Key, S = string.Join(',', g.Select(x => x.Name).Distinct()) })
            .OrderBy(x => x.K).ToList();
        var actual = db.Table<KindWordEntryRow>().GroupBy(x => x.Kind)
            .Select(g => new { K = g.Key, S = string.Join(',', g.Select(x => x.Name).Distinct()) })
            .ToList().OrderBy(x => x.K).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctJoinWithAnotherSeparatorThrows()
    {
        using TestDatabase db = Seed(out _, nameof(DistinctJoinWithAnotherSeparatorThrows));

        Assert.Throws<NotSupportedException>(() => db.Table<KindWordEntryRow>().GroupBy(x => x.Kind)
            .Select(g => new { K = g.Key, S = string.Join("|", g.Select(x => x.Name).Distinct()) })
            .ToList());
    }

    [Fact]
    public void DistinctJoinWithAColumnSeparatorThrows()
    {
        using TestDatabase db = Seed(out _, nameof(DistinctJoinWithAColumnSeparatorThrows));

        Assert.Throws<NotSupportedException>(() => db.Table<KindWordEntryRow>().GroupBy(x => x.Kind)
            .Select(g => new { K = g.Key, S = string.Join(g.Key.ToString(), g.Select(x => x.Name).Distinct()) })
            .ToList());
    }

    [Fact]
    public void DistinctJoinWithAFilterMatchesLinq()
    {
        using TestDatabase db = Seed(out List<KindWordEntryRow> rows, nameof(DistinctJoinWithAFilterMatchesLinq));

        var expected = rows.GroupBy(x => x.Kind)
            .Select(g => new { K = g.Key, S = string.Join(",", g.Where(x => x.Name != "b").Select(x => x.Name).Distinct()) })
            .OrderBy(x => x.K).ToList();
        var actual = db.Table<KindWordEntryRow>().GroupBy(x => x.Kind)
            .Select(g => new { K = g.Key, S = string.Join(",", g.Where(x => x.Name != "b").Select(x => x.Name).Distinct()) })
            .ToList().OrderBy(x => x.K).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AddRangeOnAMissingTableThrows()
    {
        using TestDatabase db = new();

        Assert.ThrowsAny<Exception>(() => db.Table<KindWordEntryRow>().AddRange([new KindWordEntryRow { Id = 1, Kind = 1, Name = "a" }]));
    }

    [Fact]
    public void DistinctConcatThrows()
    {
        using TestDatabase db = Seed(out _, nameof(DistinctConcatThrows));

        Assert.Throws<NotSupportedException>(() => db.Table<KindWordEntryRow>().GroupBy(x => x.Kind)
            .Select(g => new { K = g.Key, S = string.Concat(g.Select(x => x.Name).Distinct()) })
            .ToList());
    }
}
