using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class HbRow
{
    [Key]
    public int Id { get; set; }
    public int Value { get; set; }
    public int Grp { get; set; }
}

public class HavingBracketingTests
{
    private static readonly HbRow[] Data =
    [
        new HbRow { Id = 1, Value = 5, Grp = 1 },
        new HbRow { Id = 2, Value = 5, Grp = 2 },
        new HbRow { Id = 3, Value = 5, Grp = 2 },
        new HbRow { Id = 4, Value = 5, Grp = 7 },
        new HbRow { Id = 5, Value = 20, Grp = 9 },
        new HbRow { Id = 6, Value = 20, Grp = 9 },
        new HbRow { Id = 7, Value = 20, Grp = 9 },
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<HbRow>().Schema.CreateTable();
        db.Table<HbRow>().AddRange(Data);
        return db;
    }

    [Fact]
    public void OrTermFirst_ThenAndTerm_BracketsOr()
    {
        using TestDatabase db = CreateDb();
        List<int> expected = Data.GroupBy(x => x.Grp)
            .Where(g => g.Count() == 1 || g.Sum(x => x.Value) > 50)
            .Where(g => g.Key >= 5).Select(g => g.Key).OrderBy(k => k).ToList();
        List<int> actual = db.Table<HbRow>().GroupBy(x => x.Grp)
            .Where(g => g.Count() == 1 || g.Sum(x => x.Value) > 50)
            .Where(g => g.Key >= 5).Select(g => g.Key).OrderBy(k => k).ToList();

        Assert.Equal([7, 9], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AndTermFirst_ThenOrTerm_BracketsOr()
    {
        using TestDatabase db = CreateDb();
        List<int> expected = Data.GroupBy(x => x.Grp)
            .Where(g => g.Key >= 5)
            .Where(g => g.Count() == 1 || g.Sum(x => x.Value) > 50).Select(g => g.Key).OrderBy(k => k).ToList();
        List<int> actual = db.Table<HbRow>().GroupBy(x => x.Grp)
            .Where(g => g.Key >= 5)
            .Where(g => g.Count() == 1 || g.Sum(x => x.Value) > 50).Select(g => g.Key).OrderBy(k => k).ToList();

        Assert.Equal([7, 9], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SingleOrHaving_NoBracketingNeeded()
    {
        using TestDatabase db = CreateDb();
        List<int> expected = Data.GroupBy(x => x.Grp)
            .Where(g => g.Count() == 1 || g.Sum(x => x.Value) > 50).Select(g => g.Key).OrderBy(k => k).ToList();
        List<int> actual = db.Table<HbRow>().GroupBy(x => x.Grp)
            .Where(g => g.Count() == 1 || g.Sum(x => x.Value) > 50).Select(g => g.Key).OrderBy(k => k).ToList();

        Assert.Equal([1, 7, 9], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AndOnlyHavings_Unaffected()
    {
        using TestDatabase db = CreateDb();
        List<int> expected = Data.GroupBy(x => x.Grp)
            .Where(g => g.Count() >= 1)
            .Where(g => g.Key >= 5).Select(g => g.Key).OrderBy(k => k).ToList();
        List<int> actual = db.Table<HbRow>().GroupBy(x => x.Grp)
            .Where(g => g.Count() >= 1)
            .Where(g => g.Key >= 5).Select(g => g.Key).OrderBy(k => k).ToList();

        Assert.Equal([7, 9], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TwoOrHavings_BothBracketed()
    {
        using TestDatabase db = CreateDb();
        List<int> expected = Data.GroupBy(x => x.Grp)
            .Where(g => g.Count() == 1 || g.Key == 2)
            .Where(g => g.Sum(x => x.Value) >= 10 || g.Key == 7).Select(g => g.Key).OrderBy(k => k).ToList();
        List<int> actual = db.Table<HbRow>().GroupBy(x => x.Grp)
            .Where(g => g.Count() == 1 || g.Key == 2)
            .Where(g => g.Sum(x => x.Value) >= 10 || g.Key == 7).Select(g => g.Key).OrderBy(k => k).ToList();

        Assert.Equal([2, 7], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SingleCombinedPredicate_StillWorks()
    {
        using TestDatabase db = CreateDb();
        List<int> expected = Data.GroupBy(x => x.Grp)
            .Where(g => (g.Count() == 1 || g.Sum(x => x.Value) > 50) && g.Key >= 5).Select(g => g.Key).OrderBy(k => k).ToList();
        List<int> actual = db.Table<HbRow>().GroupBy(x => x.Grp)
            .Where(g => (g.Count() == 1 || g.Sum(x => x.Value) > 50) && g.Key >= 5).Select(g => g.Key).OrderBy(k => k).ToList();

        Assert.Equal([7, 9], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GeneratedHaving_BracketsOrTerm()
    {
        using TestDatabase db = CreateDb();
        var query = db.Table<HbRow>().GroupBy(x => x.Grp)
            .Where(g => g.Count() == 1 || g.Sum(x => x.Value) > 50)
            .Where(g => g.Key >= 5).Select(g => g.Key);
        string sql = query.ToSqlCommand().CommandText;

        Assert.Contains("HAVING (", sql);
    }
}
