using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NullableBoolBitwiseLogicTests
{
    private static (TestDatabase db, TwoNullableBoolEntity[] seed) SeedTruthTable()
    {
        bool?[] values = [null, false, true];
        List<TwoNullableBoolEntity> rows = [];
        int id = 1;
        foreach (bool? a in values)
        {
            foreach (bool? b in values)
            {
                rows.Add(new TwoNullableBoolEntity { Id = id++, A = a, B = b });
            }
        }

        TwoNullableBoolEntity[] seed = rows.ToArray();
        TestDatabase db = new();
        db.Table<TwoNullableBoolEntity>().Schema.CreateTable();
        foreach (TwoNullableBoolEntity r in seed)
        {
            db.Table<TwoNullableBoolEntity>().Add(r);
        }

        return (db, seed);
    }

    [Fact]
    public void AndProjection_FullTruthTable_MatchesLinqToObjects()
    {
        (TestDatabase db, TwoNullableBoolEntity[] seed) = SeedTruthTable();
        using TestDatabase _ = db;

        List<bool?> expected = seed.OrderBy(x => x.Id).Select(x => x.A & x.B).ToList();
        List<bool?> actual = db.Table<TwoNullableBoolEntity>().OrderBy(x => x.Id).Select(x => x.A & x.B).ToList();

        Assert.Equal([null, false, null, false, false, false, null, false, true], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrProjection_FullTruthTable_MatchesLinqToObjects()
    {
        (TestDatabase db, TwoNullableBoolEntity[] seed) = SeedTruthTable();
        using TestDatabase _ = db;

        List<bool?> expected = seed.OrderBy(x => x.Id).Select(x => x.A | x.B).ToList();
        List<bool?> actual = db.Table<TwoNullableBoolEntity>().OrderBy(x => x.Id).Select(x => x.A | x.B).ToList();

        Assert.Equal([null, null, true, null, false, true, true, true, true], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AndWhereEqualsTrue_OnlyBothTrue()
    {
        (TestDatabase db, TwoNullableBoolEntity[] seed) = SeedTruthTable();
        using TestDatabase _ = db;

        List<int> expected = seed.Where(x => (x.A & x.B) == true).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableBoolEntity>().Where(x => (x.A & x.B) == true).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([9], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrWhereEqualsFalse_OnlyBothFalse()
    {
        (TestDatabase db, TwoNullableBoolEntity[] seed) = SeedTruthTable();
        using TestDatabase _ = db;

        List<int> expected = seed.Where(x => (x.A | x.B) == false).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableBoolEntity>().Where(x => (x.A | x.B) == false).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([5], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AndWithNonNullConstant_MatchesLinqToObjects()
    {
        (TestDatabase db, TwoNullableBoolEntity[] seed) = SeedTruthTable();
        using TestDatabase _ = db;

        List<bool?> expected = seed.OrderBy(x => x.Id).Select(x => x.A & false).ToList();
        List<bool?> actual = db.Table<TwoNullableBoolEntity>().OrderBy(x => x.Id).Select(x => x.A & false).ToList();

        Assert.Equal([false, false, false, false, false, false, false, false, false], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrWithNonNullConstant_MatchesLinqToObjects()
    {
        (TestDatabase db, TwoNullableBoolEntity[] seed) = SeedTruthTable();
        using TestDatabase _ = db;

        List<bool?> expected = seed.OrderBy(x => x.Id).Select(x => x.A | true).ToList();
        List<bool?> actual = db.Table<TwoNullableBoolEntity>().OrderBy(x => x.Id).Select(x => x.A | true).ToList();

        Assert.Equal([true, true, true, true, true, true, true, true, true], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NonNullableBoolColumns_And_StillCorrect()
    {
        (TestDatabase db, TwoNullableBoolEntity[] seed) = SeedTruthTable();
        using TestDatabase _ = db;

        List<int> expected = seed.Where(x => (x.Id > 0) & (x.Id < 5)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableBoolEntity>().Where(x => (x.Id > 0) & (x.Id < 5)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 2, 3, 4], expected);
        Assert.Equal(expected, actual);
    }
}
