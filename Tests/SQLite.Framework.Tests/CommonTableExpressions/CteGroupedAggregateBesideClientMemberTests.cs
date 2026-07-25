using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21bCteGrpRow")]
public class H21bCteGrpRow
{
    [Key]
    public int Id { get; set; }

    public int G { get; set; }

    public int V { get; set; }
}

public class CteGroupedAggregateBesideClientMemberTests
{
    private static List<H21bCteGrpRow> Rows() =>
    [
        new H21bCteGrpRow { Id = 1, G = 1, V = 10 },
        new H21bCteGrpRow { Id = 2, G = 1, V = 20 },
        new H21bCteGrpRow { Id = 3, G = 2, V = 30 },
        new H21bCteGrpRow { Id = 4, G = 3, V = 40 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21bCteGrpRow>().Schema.CreateTable();
        db.Table<H21bCteGrpRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void CteGroupedCountBesideArrayMemberKeepsPerGroupCounts()
    {
        using TestDatabase db = Setup();

        List<(int Key, int Cnt)> expected = Rows()
            .GroupBy(r => r.G)
            .Select(g => new { g.Key, Cnt = g.Count(), Arr = new[] { g.Key } })
            .Select(p => (p.Key, p.Cnt))
            .OrderBy(p => p.Key)
            .ToList();

        List<(int Key, int Cnt)> actual = db.With(() => db.Table<H21bCteGrpRow>()
                .GroupBy(r => r.G)
                .Select(g => new { g.Key, Cnt = g.Count(), Arr = new[] { g.Key } }))
            .Select(x => new { x.Key, x.Cnt })
            .AsEnumerable()
            .Select(p => (p.Key, p.Cnt))
            .OrderBy(p => p.Key)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteGroupedSumBesideArrayMemberKeepsPerGroupSums()
    {
        using TestDatabase db = Setup();

        List<(int Key, int Total)> expected = Rows()
            .GroupBy(r => r.G)
            .Select(g => new { g.Key, Total = g.Sum(r => r.V), Arr = new[] { g.Key } })
            .Select(p => (p.Key, p.Total))
            .OrderBy(p => p.Key)
            .ToList();

        List<(int Key, int Total)> actual = db.With(() => db.Table<H21bCteGrpRow>()
                .GroupBy(r => r.G)
                .Select(g => new { g.Key, Total = g.Sum(r => r.V), Arr = new[] { g.Key } }))
            .Select(x => new { x.Key, x.Total })
            .AsEnumerable()
            .Select(p => (p.Key, p.Total))
            .OrderBy(p => p.Key)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
