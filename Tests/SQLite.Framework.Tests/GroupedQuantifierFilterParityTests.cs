using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupedQuantifierFilterParityTests
{
    [Fact]
    public void AllOverEmptyFilteredSubsetIsTrue()
    {
        using TestDatabase db = Seed(out List<GroupedFilterRow> mem, new[]
        {
            Row(1, 9, value: 5),
            Row(2, 9, value: 6)
        });

        bool expected = mem.GroupBy(r => r.Grp).Select(g => g.Where(x => x.Value > 1000).All(x => x.Value > 0)).Single();
        bool actual = db.Table<GroupedFilterRow>().GroupBy(r => r.Grp).Select(g => g.Where(x => x.Value > 1000).All(x => x.Value > 0)).Single();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AllOverEmptyFilterMixedGroupsMatchesObjects()
    {
        using TestDatabase db = Seed(out List<GroupedFilterRow> mem, new[]
        {
            Row(1, 1, value: 50),
            Row(2, 1, value: 60),
            Row(3, 2, value: 1),
            Row(4, 2, value: 2),
            Row(5, 3, value: 100)
        });

        List<bool> expected = mem.GroupBy(r => r.Grp).OrderBy(g => g.Key).Select(g => g.Where(x => x.Value >= 50).All(x => x.Value > 40)).ToList();
        List<bool> actual = db.Table<GroupedFilterRow>().GroupBy(r => r.Grp).OrderBy(g => g.Key).Select(g => g.Where(x => x.Value >= 50).All(x => x.Value > 40)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AllOverEmptyFilterOnBoolColumnMatchesObjects()
    {
        using TestDatabase db = Seed(out List<GroupedFilterRow> mem, new[]
        {
            Row(1, 1, value: 1, flag: false),
            Row(2, 1, value: 2, flag: false),
            Row(3, 2, value: 60, flag: true)
        });

        List<bool> expected = mem.GroupBy(r => r.Grp).OrderBy(g => g.Key).Select(g => g.Where(x => x.Value >= 50).All(x => x.Flag)).ToList();
        List<bool> actual = db.Table<GroupedFilterRow>().GroupBy(r => r.Grp).OrderBy(g => g.Key).Select(g => g.Where(x => x.Value >= 50).All(x => x.Flag)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AllOverEmptyFilterAllNullColumnMatchesObjects()
    {
        using TestDatabase db = Seed(out List<GroupedFilterRow> mem, new[]
        {
            Row(1, 1, nullableValue: null),
            Row(2, 1, nullableValue: null),
            Row(3, 2, nullableValue: 5)
        });

        List<bool> expected = mem.GroupBy(r => r.Grp).OrderBy(g => g.Key).Select(g => g.Where(x => x.NullableValue > 0).All(x => x.NullableValue > 0)).ToList();
        List<bool> actual = db.Table<GroupedFilterRow>().GroupBy(r => r.Grp).OrderBy(g => g.Key).Select(g => g.Where(x => x.NullableValue > 0).All(x => x.NullableValue > 0)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AllOverEmptyFilterWithKeyProjectionMatchesObjects()
    {
        using TestDatabase db = Seed(out List<GroupedFilterRow> mem, new[]
        {
            Row(1, 1, value: 1),
            Row(2, 1, value: 2),
            Row(3, 2, value: 70),
            Row(4, 2, value: 80)
        });

        var expected = mem.GroupBy(r => r.Grp).OrderBy(g => g.Key).Select(g => new { g.Key, AllPositive = g.Where(x => x.Value > 50).All(x => x.Value > 0) }).ToList();
        var actual = db.Table<GroupedFilterRow>().GroupBy(r => r.Grp).OrderBy(g => g.Key).Select(g => new { g.Key, AllPositive = g.Where(x => x.Value > 50).All(x => x.Value > 0) }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnyAndAllInSameProjectionOverEmptyFilterMatchesObjects()
    {
        using TestDatabase db = Seed(out List<GroupedFilterRow> mem, new[]
        {
            Row(1, 1, value: 3),
            Row(2, 1, value: 4),
            Row(3, 2, value: 80),
            Row(4, 2, value: 90)
        });

        var expected = mem.GroupBy(r => r.Grp).OrderBy(g => g.Key)
            .Select(g => new { g.Key, AnyHigh = g.Where(x => x.Value > 50).Any(x => x.Value > 0), AllHigh = g.Where(x => x.Value > 50).All(x => x.Value > 0) }).ToList();
        var actual = db.Table<GroupedFilterRow>().GroupBy(r => r.Grp).OrderBy(g => g.Key)
            .Select(g => new { g.Key, AnyHigh = g.Where(x => x.Value > 50).Any(x => x.Value > 0), AllHigh = g.Where(x => x.Value > 50).All(x => x.Value > 0) }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnyOverEmptyFilteredSubsetIsFalse()
    {
        using TestDatabase db = Seed(out List<GroupedFilterRow> mem, new[]
        {
            Row(1, 7, value: 5),
            Row(2, 7, value: 6)
        });

        bool expected = mem.GroupBy(r => r.Grp).Select(g => g.Where(x => x.Value > 1000).Any(x => x.Value > 0)).Single();
        bool actual = db.Table<GroupedFilterRow>().GroupBy(r => r.Grp).Select(g => g.Where(x => x.Value > 1000).Any(x => x.Value > 0)).Single();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereCountOverEmptyFilteredSubsetIsZero()
    {
        using TestDatabase db = Seed(out List<GroupedFilterRow> mem, new[]
        {
            Row(1, 1, value: 1),
            Row(2, 1, value: 2),
            Row(3, 2, value: 3)
        });

        List<int> expected = mem.GroupBy(r => r.Grp).OrderBy(g => g.Key).Select(g => g.Where(x => x.Value > 1000).Count()).ToList();
        List<int> actual = db.Table<GroupedFilterRow>().GroupBy(r => r.Grp).OrderBy(g => g.Key).Select(g => g.Where(x => x.Value > 1000).Count()).ToList();

        Assert.Equal(expected, actual);
    }

    private static GroupedFilterRow Row(int id, int grp, int value = 0, int? nullableValue = null, bool flag = false)
    {
        return new GroupedFilterRow { Id = id, Grp = grp, Value = value, NullableValue = nullableValue, Flag = flag };
    }

    private static TestDatabase Seed(out List<GroupedFilterRow> mem, IEnumerable<GroupedFilterRow> rows)
    {
        TestDatabase db = new();
        db.Table<GroupedFilterRow>().Schema.CreateTable();
        mem = rows.ToList();
        foreach (GroupedFilterRow r in mem)
        {
            db.Table<GroupedFilterRow>().Add(r);
        }

        return db;
    }
}

[Table("GroupedFilterRows")]
public sealed class GroupedFilterRow
{
    [Key]
    public int Id { get; set; }

    public int Grp { get; set; }

    public int Value { get; set; }

    public int? NullableValue { get; set; }

    public bool Flag { get; set; }
}
