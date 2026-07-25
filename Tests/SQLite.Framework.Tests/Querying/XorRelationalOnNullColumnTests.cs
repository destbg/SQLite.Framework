using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class XorRelationalOnNullColumnTests
{
    private static (TestDatabase db, TwoNullableIntEntity[] seed) SeedPairs()
    {
        TwoNullableIntEntity[] seed =
        [
            new TwoNullableIntEntity { Id = 1, A = null, B = null },
            new TwoNullableIntEntity { Id = 2, A = 5, B = 5 },
            new TwoNullableIntEntity { Id = 3, A = 5, B = 7 },
            new TwoNullableIntEntity { Id = 4, A = null, B = 5 },
        ];
        TestDatabase db = new();
        db.Table<TwoNullableIntEntity>().Schema.CreateTable();
        foreach (TwoNullableIntEntity r in seed)
        {
            db.Table<TwoNullableIntEntity>().Add(r);
        }

        return (db, seed);
    }

    [Fact]
    public void RelationalXorEquality_NullRowsKept()
    {
        (TestDatabase db, TwoNullableIntEntity[] seed) = SeedPairs();
        using TestDatabase _ = db;

        List<int> expected = seed.Where(x => (x.A > 5) ^ (x.B == 5)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableIntEntity>().Where(x => (x.A > 5) ^ (x.B == 5)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([2, 4], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RelationalXorTrue_AllRowsKept()
    {
        (TestDatabase db, TwoNullableIntEntity[] seed) = SeedPairs();
        using TestDatabase _ = db;

        List<int> expected = seed.Where(x => (x.A > 5) ^ true).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableIntEntity>().Where(x => (x.A > 5) ^ true).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 2, 3, 4], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RelationalXorFalse_NoRowsKept()
    {
        (TestDatabase db, TwoNullableIntEntity[] seed) = SeedPairs();
        using TestDatabase _ = db;

        List<int> expected = seed.Where(x => (x.A > 5) ^ false).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableIntEntity>().Where(x => (x.A > 5) ^ false).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EqualityXorRelational_RelationalOnRight_NullRowsKept()
    {
        (TestDatabase db, TwoNullableIntEntity[] seed) = SeedPairs();
        using TestDatabase _ = db;

        List<int> expected = seed.Where(x => (x.B == 5) ^ (x.A > 5)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableIntEntity>().Where(x => (x.B == 5) ^ (x.A > 5)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([2, 4], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RelationalXorRelational_BothNullableColumns_NullRowsKept()
    {
        (TestDatabase db, TwoNullableIntEntity[] seed) = SeedPairs();
        using TestDatabase _ = db;

        List<int> expected = seed.Where(x => (x.A > 5) ^ (x.B > 3)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableIntEntity>().Where(x => (x.A > 5) ^ (x.B > 3)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([2, 3, 4], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RelationalXorTrue_NonNullableColumn_StillCorrect()
    {
        (int Id, int? Value)[] seed = [(1, 1), (2, 2), (3, 3)];
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        foreach ((int id, int? value) in seed)
        {
            db.Table<NullableEntity>().Add(new NullableEntity { Id = id, Value = value });
        }

        List<int> expected = seed.Where(x => (x.Id > 2) ^ true).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NullableEntity>().Where(x => (x.Id > 2) ^ true).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 2], expected);
        Assert.Equal(expected, actual);
    }
}
