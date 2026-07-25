using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("h20agg_rows")]
public class H20AggRow
{
    [Key]
    public int Id { get; set; }

    public int? Amount { get; set; }
}

public class H20AggPart
{
    public bool V { get; set; }
}

public class H20AggOuter
{
    public H20AggPart? N { get; set; }
}

public class GroupByNestedKeyLiftedComparisonParityTests
{
    private static List<H20AggRow> RowData()
    {
        return
        [
            new H20AggRow { Id = 1, Amount = null },
            new H20AggRow { Id = 2, Amount = null },
            new H20AggRow { Id = 3, Amount = 1 },
            new H20AggRow { Id = 4, Amount = 10 },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<H20AggRow>().Schema.CreateTable();
        db.Table<H20AggRow>().AddRange(RowData());
        return db;
    }

    [Fact]
    public void MemberInitPartLiftedComparisonGroupsNullWithFalse()
    {
        using TestDatabase db = Seed();

        List<(bool V, int C)> expected = RowData()
            .GroupBy(r => r.Amount > 5)
            .Select(g => (g.Key, g.Count()))
            .OrderBy(x => x.Key)
            .ToList();

        List<(bool V, int C)> actual = db.Table<H20AggRow>()
            .GroupBy(r => new { W = new H20AggPart { V = r.Amount > 5 } })
            .Select(g => new { g.Key.W.V, C = g.Count() })
            .AsEnumerable()
            .Select(x => (x.V, x.C))
            .OrderBy(x => x.V)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedAnonymousLiftedComparisonGroupsNullWithFalse()
    {
        using TestDatabase db = Seed();

        List<(bool V, int C)> expected = RowData()
            .GroupBy(r => r.Amount > 5)
            .Select(g => (g.Key, g.Count()))
            .OrderBy(x => x.Key)
            .ToList();

        List<(bool V, int C)> actual = db.Table<H20AggRow>()
            .GroupBy(r => new { W = new { V = r.Amount > 5 } })
            .Select(g => new { g.Key.W.V, C = g.Count() })
            .AsEnumerable()
            .Select(x => (x.V, x.C))
            .OrderBy(x => x.V)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DeepNestedLiftedComparisonGroupsNullWithFalse()
    {
        using TestDatabase db = Seed();

        List<(bool V, int C)> expected = RowData()
            .GroupBy(r => r.Amount > 5)
            .Select(g => (g.Key, g.Count()))
            .OrderBy(x => x.Key)
            .ToList();

        List<(bool V, int C)> actual = db.Table<H20AggRow>()
            .GroupBy(r => new { W = new H20AggOuter { N = new H20AggPart { V = r.Amount > 5 } } })
            .Select(g => new { V = g.Key.W!.N!.V, C = g.Count() })
            .AsEnumerable()
            .Select(x => (x.V, x.C))
            .OrderBy(x => x.V)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumOverNestedLiftedComparisonKeyGroupsNullWithFalse()
    {
        using TestDatabase db = Seed();

        List<(bool V, int? S)> expected = RowData()
            .GroupBy(r => r.Amount > 5)
            .Select(g => (g.Key, g.Sum(r => r.Amount)))
            .OrderBy(x => x.Key)
            .ToList();

        List<(bool V, int? S)> actual = db.Table<H20AggRow>()
            .GroupBy(r => new { W = new H20AggPart { V = r.Amount > 5 } })
            .Select(g => new { g.Key.W.V, S = g.Sum(r => r.Amount) })
            .AsEnumerable()
            .Select(x => (x.V, x.S))
            .OrderBy(x => x.V)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByNestedLiftedComparisonKeyKeepsOneFalseGroup()
    {
        using TestDatabase db = Seed();

        List<bool> expected = RowData()
            .GroupBy(r => r.Amount > 5)
            .OrderByDescending(g => g.Key)
            .Select(g => g.Key)
            .ToList();

        List<bool> actual = db.Table<H20AggRow>()
            .GroupBy(r => new { W = new H20AggPart { V = r.Amount > 5 } })
            .OrderByDescending(g => g.Key.W.V)
            .Select(g => g.Key.W.V)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
