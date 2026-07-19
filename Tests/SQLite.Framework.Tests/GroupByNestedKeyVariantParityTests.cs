using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21GroupRows")]
public class H21GroupRow
{
    [Key]
    public int Id { get; set; }

    public int Score { get; set; }

    public int? Amount { get; set; }
}

public class H21GroupPart
{
    public bool V { get; set; }
}

public class H21GroupOuter
{
    public H21GroupPart Part { get; } = new();
}

public class GroupByNestedKeyVariantParityTests
{
    private static List<H21GroupRow> RowData()
    {
        return
        [
            new H21GroupRow { Id = 1, Score = 0, Amount = null },
            new H21GroupRow { Id = 2, Score = 5, Amount = null },
            new H21GroupRow { Id = 3, Score = 6, Amount = 1 },
            new H21GroupRow { Id = 4, Score = -1, Amount = 10 },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<H21GroupRow>().Schema.CreateTable();
        db.Table<H21GroupRow>().AddRange(RowData());
        return db;
    }

    [Fact]
    public void MemberGroupBindingKeyGroupsByTheNestedComparison()
    {
        using TestDatabase db = Seed();

        List<(int S, int C)> expected = RowData()
            .GroupBy(r => r.Score > 5)
            .Select(g => (S: g.Sum(r => r.Score), C: g.Count()))
            .OrderBy(x => x.S)
            .ToList();

        List<(int S, int C)> actual = db.Table<H21GroupRow>()
            .GroupBy(r => new { W = new H21GroupOuter { Part = { V = r.Score > 5 } } })
            .Select(g => new { S = g.Sum(r => r.Score), C = g.Count() })
            .AsEnumerable()
            .Select(x => (x.S, x.C))
            .OrderBy(x => x.S)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupKeyWithArrayMemberGroupsByTheScalarKey()
    {
        using TestDatabase db = Seed();

        List<(bool K, int C)> expected = RowData()
            .GroupBy(r => r.Amount > 5)
            .Select(g => (g.Key, g.Count()))
            .OrderBy(x => x.Key)
            .ToList();

        List<(bool K, int C)> actual = db.Table<H21GroupRow>()
            .GroupBy(r => new { K = r.Amount > 5, Arr = new[] { 7 } })
            .Select(g => new { g.Key.K, C = g.Count() })
            .AsEnumerable()
            .Select(x => (x.K, x.C))
            .OrderBy(x => x.K)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
