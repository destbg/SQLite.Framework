using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupingNoSelectorAggregateParityTests
{
    private static TestDatabase Create()
    {
        TestDatabase db = new();
        db.Table<GnsRow>().Schema.CreateTable();
        foreach (GnsRow r in Seed)
        {
            db.Table<GnsRow>().Add(r);
        }
        return db;
    }

    private static readonly GnsRow[] Seed =
    [
        new GnsRow { Id = 1, N = 1, M = 10 },
        new GnsRow { Id = 2, N = 2, M = 20 },
        new GnsRow { Id = 3, N = 3, M = 30 },
        new GnsRow { Id = 4, N = 4, M = 40 },
        new GnsRow { Id = 5, N = 2, M = 50 },
        new GnsRow { Id = 6, N = 4, M = 60 },
    ];

    [Fact]
    public void ProjectedScalarGroupBySumNoSelector_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.Select(r => r.N).GroupBy(x => x % 2).Select(g => g.Sum()).OrderBy(s => s).ToList();
        List<int> actual = db.Table<GnsRow>().Select(r => r.N).GroupBy(x => x % 2).Select(g => g.Sum()).OrderBy(s => s).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ProjectedScalarGroupByMaxNoSelector_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.Select(r => r.N).GroupBy(x => x % 2).Select(g => g.Max()).OrderBy(s => s).ToList();
        List<int> actual = db.Table<GnsRow>().Select(r => r.N).GroupBy(x => x % 2).Select(g => g.Max()).OrderBy(s => s).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ProjectedScalarGroupByMinNoSelector_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.Select(r => r.M).GroupBy(x => x % 2).Select(g => g.Min()).OrderBy(s => s).ToList();
        List<int> actual = db.Table<GnsRow>().Select(r => r.M).GroupBy(x => x % 2).Select(g => g.Min()).OrderBy(s => s).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ProjectedScalarGroupByAverageNoSelector_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<double> oracle = Seed.Select(r => r.N).GroupBy(x => x % 2).Select(g => g.Average()).OrderBy(s => s).ToList();
        List<double> actual = db.Table<GnsRow>().Select(r => r.N).GroupBy(x => x % 2).Select(g => g.Average()).OrderBy(s => s).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ComputedScalarGroupBySumNoSelector_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.Select(r => r.M + 1).GroupBy(x => x % 2).Select(g => g.Sum()).OrderBy(s => s).ToList();
        List<int> actual = db.Table<GnsRow>().Select(r => r.M + 1).GroupBy(x => x % 2).Select(g => g.Sum()).OrderBy(s => s).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SelectManyScalarGroupBySumNoSelector_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.SelectMany(a => Seed, (a, b) => a.M).GroupBy(x => x % 30).Select(g => g.Sum()).OrderBy(s => s).ToList();
        List<int> actual = db.Table<GnsRow>().SelectMany(a => db.Table<GnsRow>(), (a, b) => a.M).GroupBy(x => x % 30).Select(g => g.Sum()).OrderBy(s => s).ToList();

        Assert.Equal(oracle, actual);
    }
}

[Table("GnsRow")]
public class GnsRow
{
    [Key]
    public int Id { get; set; }

    public int N { get; set; }

    public int M { get; set; }
}
