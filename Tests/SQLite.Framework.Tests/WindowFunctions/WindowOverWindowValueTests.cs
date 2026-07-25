using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21gTierRows")]
public class H21gTierRow
{
    [Key]
    public int Id { get; set; }

    public int Grp { get; set; }

    public int Amount { get; set; }
}

public class WindowOverWindowValueTests
{
    [Fact]
    public void RankOrderedByInnerWindowTotalMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<H21gTierRow> local = Rows();
        Dictionary<int, int> totals = local
            .GroupBy(r => r.Grp)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

        List<(int Id, long Rank)> expected = local
            .OrderBy(r => r.Id)
            .Select(r => (Id: r.Id, Rank: (long)local.Count(o => totals[o.Grp] > totals[r.Grp]) + 1))
            .ToList();

        List<(int Id, long Rank)> actual = db.Table<H21gTierRow>()
            .Select(x => new
            {
                x.Id,
                x.Grp,
                Total = SQLiteWindowFunctions.Sum(x.Amount).Over().PartitionBy(x.Grp).AsValue()
            })
            .Select(y => new
            {
                y.Id,
                Rank = SQLiteWindowFunctions.Rank().Over().OrderByDescending(y.Total).AsValue()
            })
            .AsEnumerable()
            .Select(a => (Id: a.Id, Rank: a.Rank))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumOfInnerWindowTotalMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<H21gTierRow> local = Rows();
        Dictionary<int, int> totals = local
            .GroupBy(r => r.Grp)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

        int outerTotal = local.Sum(r => totals[r.Grp]);

        List<(int Id, int Total)> expected = local
            .OrderBy(r => r.Id)
            .Select(r => (Id: r.Id, Total: outerTotal))
            .ToList();

        List<(int Id, int Total)> actual = db.Table<H21gTierRow>()
            .Select(x => new
            {
                x.Id,
                x.Grp,
                Total = SQLiteWindowFunctions.Sum(x.Amount).Over().PartitionBy(x.Grp).AsValue()
            })
            .Select(y => new
            {
                y.Id,
                Outer = SQLiteWindowFunctions.Sum(y.Total).Over().AsValue()
            })
            .AsEnumerable()
            .Select(a => (Id: a.Id, Total: a.Outer))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H21gTierRow> Rows()
    {
        return
        [
            new H21gTierRow { Id = 1, Grp = 1, Amount = 10 },
            new H21gTierRow { Id = 2, Grp = 1, Amount = 20 },
            new H21gTierRow { Id = 3, Grp = 2, Amount = 5 },
            new H21gTierRow { Id = 4, Grp = 3, Amount = 30 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21gTierRow>().Schema.CreateTable();
        db.Table<H21gTierRow>().AddRange(Rows());
        return db;
    }
}
