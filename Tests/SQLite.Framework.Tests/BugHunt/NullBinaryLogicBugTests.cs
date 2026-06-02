using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

internal sealed class BugHuntBoolPair
{
    [Key]
    public int Id { get; set; }

    public bool? F1 { get; set; }

    public bool? F2 { get; set; }
}

public class NullBinaryLogicBugTests
{
    [Fact]
    public void RelationalEqualsBoolConstant_OnNullColumn_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        db.Table<NullableEntity>().Add(new NullableEntity { Id = 1, Value = null });
        db.Table<NullableEntity>().Add(new NullableEntity { Id = 2, Value = 10 });
        db.Table<NullableEntity>().Add(new NullableEntity { Id = 3, Value = 3 });

        (int Id, int? Value)[] seed = [(1, null), (2, 10), (3, 3)];
        List<int> expected = seed.Where(x => (x.Value > 5) == false).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NullableEntity>().Where(x => (x.Value > 5) == false).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void XorOfRelationalAndEquality_OnNullColumn_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<TwoNullableIntEntity>().Schema.CreateTable();
        TwoNullableIntEntity[] rows =
        [
            new TwoNullableIntEntity { Id = 1, A = null, B = null },
            new TwoNullableIntEntity { Id = 2, A = 5, B = 5 },
            new TwoNullableIntEntity { Id = 3, A = 5, B = 7 },
            new TwoNullableIntEntity { Id = 4, A = null, B = 5 },
        ];
        foreach (TwoNullableIntEntity r in rows)
        {
            db.Table<TwoNullableIntEntity>().Add(r);
        }

        List<int> expected = rows.Where(x => (x.A > 5) ^ (x.B == 5)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableIntEntity>().Where(x => (x.A > 5) ^ (x.B == 5)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void XorOfRelationalAndTrue_OnNullColumn_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<TwoNullableIntEntity>().Schema.CreateTable();
        TwoNullableIntEntity[] rows =
        [
            new TwoNullableIntEntity { Id = 1, A = null, B = null },
            new TwoNullableIntEntity { Id = 2, A = 5, B = 5 },
            new TwoNullableIntEntity { Id = 3, A = 5, B = 7 },
            new TwoNullableIntEntity { Id = 4, A = null, B = 5 },
        ];
        foreach (TwoNullableIntEntity r in rows)
        {
            db.Table<TwoNullableIntEntity>().Add(r);
        }

        List<int> expected = rows.Where(x => (x.A > 5) ^ true).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableIntEntity>().Where(x => (x.A > 5) ^ true).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BitwiseAnd_OnNullableBoolColumns_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<BugHuntBoolPair>().Schema.CreateTable();
        db.Table<BugHuntBoolPair>().Add(new BugHuntBoolPair { Id = 1, F1 = null, F2 = false });
        db.Table<BugHuntBoolPair>().Add(new BugHuntBoolPair { Id = 2, F1 = true, F2 = false });

        BugHuntBoolPair[] seed =
        [
            new BugHuntBoolPair { Id = 1, F1 = null, F2 = false },
            new BugHuntBoolPair { Id = 2, F1 = true, F2 = false },
        ];

        List<bool?> expected = seed.OrderBy(x => x.Id).Select(x => x.F1 & x.F2).ToList();
        List<bool?> actual = db.Table<BugHuntBoolPair>().OrderBy(x => x.Id).Select(x => x.F1 & x.F2).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BitwiseOr_OnNullableBoolColumns_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<BugHuntBoolPair>().Schema.CreateTable();
        db.Table<BugHuntBoolPair>().Add(new BugHuntBoolPair { Id = 1, F1 = null, F2 = true });
        db.Table<BugHuntBoolPair>().Add(new BugHuntBoolPair { Id = 2, F1 = false, F2 = true });

        BugHuntBoolPair[] seed =
        [
            new BugHuntBoolPair { Id = 1, F1 = null, F2 = true },
            new BugHuntBoolPair { Id = 2, F1 = false, F2 = true },
        ];

        List<bool?> expected = seed.OrderBy(x => x.Id).Select(x => x.F1 | x.F2).ToList();
        List<bool?> actual = db.Table<BugHuntBoolPair>().OrderBy(x => x.Id).Select(x => x.F1 | x.F2).ToList();

        Assert.Equal(expected, actual);
    }
}
