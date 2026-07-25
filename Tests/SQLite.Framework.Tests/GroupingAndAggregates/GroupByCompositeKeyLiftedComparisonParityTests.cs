using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CompositeKeyLiftedRow
{
    [Key] public int Id { get; set; }
    public int? A { get; set; }
}

public class GroupByCompositeKeyLiftedComparisonParityTests
{
    private static TestDatabase Seed(out List<CompositeKeyLiftedRow> rows)
    {
        TestDatabase db = new();
        db.Table<CompositeKeyLiftedRow>().Schema.CreateTable();
        rows = new()
        {
            new() { Id = 1, A = 10 },
            new() { Id = 2, A = null },
            new() { Id = 3, A = -5 },
            new() { Id = 4, A = 0 },
            new() { Id = 5, A = null },
        };
        foreach (CompositeKeyLiftedRow r in rows)
        {
            db.Table<CompositeKeyLiftedRow>().Add(r);
        }

        return db;
    }

    [Fact]
    public void GroupBy_AnonymousKeyWithLiftedComparison_FoldsNullIntoFalseGroup()
    {
        using TestDatabase db = Seed(out List<CompositeKeyLiftedRow> rows);
        int expected = rows.GroupBy(r => new { F = r.A > 0, P = r.Id % 2 }).Count();
        int actual = db.Table<CompositeKeyLiftedRow>().GroupBy(r => new { F = r.A > 0, P = r.Id % 2 }).Count();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupBy_AnonymousKeyWithLiftedComparison_MatchesPerGroupCounts()
    {
        using TestDatabase db = Seed(out List<CompositeKeyLiftedRow> rows);
        List<int> expected = rows.GroupBy(r => new { F = r.A > 0 }).Select(g => g.Count()).OrderBy(x => x).ToList();
        List<int> actual = db.Table<CompositeKeyLiftedRow>().GroupBy(r => new { F = r.A > 0 }).Select(g => g.Count()).OrderBy(x => x).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupBy_NestedAnonymousKey_MatchesPerGroupCounts()
    {
        using TestDatabase db = Seed(out List<CompositeKeyLiftedRow> rows);
        List<int> expected = rows.GroupBy(r => new { r.A, Inner = new { Mod = r.Id % 2 } }).OrderBy(g => g.Key.A).ThenBy(g => g.Key.Inner.Mod).Select(g => g.Count()).ToList();
        List<int> actual = db.Table<CompositeKeyLiftedRow>().GroupBy(r => new { r.A, Inner = new { Mod = r.Id % 2 } }).OrderBy(g => g.Key.A).ThenBy(g => g.Key.Inner.Mod).Select(g => g.Count()).ToList();
        Assert.Equal(expected, actual);
    }
}
