using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21mAggRows")]
public class H21mAggRow
{
    [Key]
    public int Id { get; set; }

    public int Grp { get; set; }

    public int? Amount { get; set; }
}

public class AggregateLiftedComparisonSelectorTests
{
    [Fact]
    public void MinOverLiftedComparisonSelectorMatchesLinq()
    {
        using TestDatabase db = Seed();
        List<H21mAggRow> local = Rows();

        bool? expected = local.Where(r => r.Grp == 1).Min(r => (bool?)(r.Amount > 5));

        bool? actual = db.Table<H21mAggRow>().Where(r => r.Grp == 1).Min(r => (bool?)(r.Amount > 5));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MinOverPlainBoolComparisonSelectorMatchesLinq()
    {
        using TestDatabase db = Seed();
        List<H21mAggRow> local = Rows();

        bool expected = local.Where(r => r.Grp == 1).Min(r => r.Amount > 5);

        bool actual = db.Table<H21mAggRow>().Where(r => r.Grp == 1).Min(r => r.Amount > 5);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxOverLiftedComparisonSelectorMatchesLinq()
    {
        using TestDatabase db = Seed();
        List<H21mAggRow> local = Rows();

        bool? expected = local.Where(r => r.Grp == 3).Max(r => (bool?)(r.Amount > 5));

        bool? actual = db.Table<H21mAggRow>().Where(r => r.Grp == 3).Max(r => (bool?)(r.Amount > 5));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupedMinOverLiftedComparisonSelectorMatchesLinq()
    {
        using TestDatabase db = Seed();
        List<H21mAggRow> local = Rows();

        List<(int Key, bool? M)> expected = local
            .GroupBy(r => r.Grp)
            .Select(g => (g.Key, M: g.Min(r => (bool?)(r.Amount > 5))))
            .OrderBy(x => x.Key)
            .ToList();

        List<(int Key, bool? M)> actual = db.Table<H21mAggRow>()
            .GroupBy(r => r.Grp)
            .Select(g => new { g.Key, M = g.Min(r => (bool?)(r.Amount > 5)) })
            .AsEnumerable()
            .Select(x => (x.Key, x.M))
            .OrderBy(x => x.Key)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H21mAggRow> Rows()
    {
        return
        [
            new H21mAggRow { Id = 1, Grp = 1, Amount = null },
            new H21mAggRow { Id = 2, Grp = 1, Amount = 8 },
            new H21mAggRow { Id = 3, Grp = 2, Amount = 1 },
            new H21mAggRow { Id = 4, Grp = 2, Amount = 2 },
            new H21mAggRow { Id = 5, Grp = 3, Amount = null }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<H21mAggRow>().Schema.CreateTable();
        db.Table<H21mAggRow>().AddRange(Rows());
        return db;
    }
}
