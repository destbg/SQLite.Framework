using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ULongCmpRows")]
file sealed class ULongCmpRow
{
    [Key]
    public int Id { get; set; }

    public ulong Value { get; set; }
}

public class ULongComparisonTests
{
    [Fact]
    public void GreaterThanComparisonAboveLongMaxMatchesDotNet()
    {
        (int id, ulong val)[] rows = { (1, ulong.MaxValue), (2, 5) };
        using TestDatabase db = new();
        db.Table<ULongCmpRow>().Schema.CreateTable();
        foreach ((int id, ulong val) in rows)
        {
            db.Table<ULongCmpRow>().Add(new ULongCmpRow { Id = id, Value = val });
        }

        List<int> actual = db.Table<ULongCmpRow>().Where(x => x.Value > 100).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> oracle = rows.Where(r => r.val > 100).Select(r => r.id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void OrderByAboveLongMaxMatchesDotNet()
    {
        (int id, ulong val)[] rows = { (1, ulong.MaxValue), (2, 1), (3, 5) };
        using TestDatabase db = new();
        db.Table<ULongCmpRow>().Schema.CreateTable();
        foreach ((int id, ulong val) in rows)
        {
            db.Table<ULongCmpRow>().Add(new ULongCmpRow { Id = id, Value = val });
        }

        List<int> actual = db.Table<ULongCmpRow>().OrderBy(x => x.Value).Select(x => x.Id).ToList();
        List<int> oracle = rows.OrderBy(r => r.val).Select(r => r.id).ToList();

        Assert.Equal(oracle, actual);
    }

    private static readonly ulong[] Spread =
    {
        0UL, 1UL, 1000UL, (ulong)long.MaxValue, 1UL << 63, (1UL << 63) + 1UL, ulong.MaxValue - 1UL, ulong.MaxValue
    };

    private static (TestDatabase, ulong[]) SeedSpread()
    {
        TestDatabase db = new();
        db.Table<ULongCmpRow>().Schema.CreateTable();
        for (int i = 0; i < Spread.Length; i++)
        {
            db.Table<ULongCmpRow>().Add(new ULongCmpRow { Id = i + 1, Value = Spread[i] });
        }

        return (db, Spread);
    }

    [Fact]
    public void AllComparisonOperatorsAcrossSignBitMatchDotNet()
    {
        (TestDatabase db, ulong[] mem) = SeedSpread();
        using TestDatabase _ = db;

        foreach (ulong threshold in Spread)
        {
            ulong t = threshold;
            Assert.Equal(
                mem.Where(v => v < t).OrderBy(v => v).ToList(),
                db.Table<ULongCmpRow>().Where(x => x.Value < t).Select(x => x.Value).OrderBy(v => v).ToList());
            Assert.Equal(
                mem.Where(v => v <= t).OrderBy(v => v).ToList(),
                db.Table<ULongCmpRow>().Where(x => x.Value <= t).Select(x => x.Value).OrderBy(v => v).ToList());
            Assert.Equal(
                mem.Where(v => v > t).OrderBy(v => v).ToList(),
                db.Table<ULongCmpRow>().Where(x => x.Value > t).Select(x => x.Value).OrderBy(v => v).ToList());
            Assert.Equal(
                mem.Where(v => v >= t).OrderBy(v => v).ToList(),
                db.Table<ULongCmpRow>().Where(x => x.Value >= t).Select(x => x.Value).OrderBy(v => v).ToList());
            Assert.Equal(
                mem.Where(v => v == t).OrderBy(v => v).ToList(),
                db.Table<ULongCmpRow>().Where(x => x.Value == t).Select(x => x.Value).OrderBy(v => v).ToList());
        }
    }

    [Fact]
    public void OrderByAscendingAndDescendingAcrossSignBitMatchDotNet()
    {
        (TestDatabase db, ulong[] mem) = SeedSpread();
        using TestDatabase _ = db;

        Assert.Equal(
            mem.OrderBy(v => v).ToList(),
            db.Table<ULongCmpRow>().OrderBy(x => x.Value).Select(x => x.Value).ToList());
        Assert.Equal(
            mem.OrderByDescending(v => v).ToList(),
            db.Table<ULongCmpRow>().OrderByDescending(x => x.Value).Select(x => x.Value).ToList());
    }
}
