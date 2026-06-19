using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("GrpEdgeRows")]
public class GrpEdgeRow
{
    [Key] [Column("Id")] public int Id { get; set; }
    [Column("GroupKey")] public int GroupKey { get; set; }
    [Column("Name")] public string Name { get; set; } = "";
    [Column("IntValue")] public int IntValue { get; set; }
    [Column("LongValue")] public long LongValue { get; set; }
    [Column("FloatValue")] public float FloatValue { get; set; }
    [Column("DoubleValue")] public double DoubleValue { get; set; }
    [Column("DecimalValue")] public decimal DecimalValue { get; set; }
    [Column("Flag")] public bool Flag { get; set; }
    [Column("NullableInt")] public int? NullableInt { get; set; }
    [Column("NullableKey")] public int? NullableKey { get; set; }
}

public class GroupedQuantifierAndResultSelectorParityTests
{
    private static TestDatabase Build(out List<GrpEdgeRow> mem, IEnumerable<GrpEdgeRow> rows)
    {
        TestDatabase db = new();
        db.Table<GrpEdgeRow>().Schema.CreateTable();
        mem = rows.ToList();
        foreach (GrpEdgeRow r in mem) { db.Table<GrpEdgeRow>().Add(r); }
        return db;
    }

    private static GrpEdgeRow R(int id, int key, string name = "", int i = 0, long l = 0,
        float f = 0, double d = 0, decimal dec = 0, bool flag = false, int? ni = null, int? nk = 0)
    {
        return new GrpEdgeRow { Id = id, GroupKey = key, Name = name, IntValue = i, LongValue = l,
            FloatValue = f, DoubleValue = d, DecimalValue = dec, Flag = flag, NullableInt = ni, NullableKey = nk };
    }

    [Fact]
    public void AnyAndAllPerGroup()
    {
        using TestDatabase db = Build(out List<GrpEdgeRow> mem, new[] { R(1, 1, i: 10), R(2, 1, i: 20), R(3, 2, i: 5) });
        var expected = mem.GroupBy(r => r.GroupKey).Select(g => new { g.Key, AnyBig = g.Any(x => x.IntValue > 15), AllBig = g.All(x => x.IntValue > 15) }).OrderBy(x => x.Key).ToList();
        var actual = db.Table<GrpEdgeRow>().GroupBy(r => r.GroupKey).Select(g => new { g.Key, AnyBig = g.Any(x => x.IntValue > 15), AllBig = g.All(x => x.IntValue > 15) }).OrderBy(x => x.Key).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupByResultSelectorWithAggregate()
    {
        using TestDatabase db = Build(out List<GrpEdgeRow> mem, new[] { R(1, 1, i: 10), R(2, 1, i: 20), R(3, 2, i: 30) });
        var expected = mem.GroupBy(r => r.GroupKey, (k, items) => new { Key = k, Total = items.Sum(x => x.IntValue) }).OrderBy(x => x.Key).ToList();
        var actual = db.Table<GrpEdgeRow>().GroupBy(r => r.GroupKey, (k, items) => new { Key = k, Total = items.Sum(x => x.IntValue) }).OrderBy(x => x.Key).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AllOverNullablePredicateThreeValued()
    {
        using TestDatabase db = Build(out List<GrpEdgeRow> mem, new[] { R(1, 1, ni: 5), R(2, 1, ni: null) });
        List<bool> expected = mem.GroupBy(r => r.GroupKey).Select(g => g.All(x => x.NullableInt > 0)).ToList();
        List<bool> actual = db.Table<GrpEdgeRow>().GroupBy(r => r.GroupKey).Select(g => g.All(x => x.NullableInt > 0)).ToList();
        Assert.Equal(expected, actual);
    }
}
