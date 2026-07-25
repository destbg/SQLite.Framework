using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RelationalEqualsBoolOnNullColumnTests
{
    private static (TestDatabase db, (int Id, int? Value)[] seed) SeedInts()
    {
        (int Id, int? Value)[] seed = [(1, null), (2, 10), (3, 3)];
        TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        foreach ((int id, int? value) in seed)
        {
            db.Table<NullableEntity>().Add(new NullableEntity { Id = id, Value = value });
        }

        return (db, seed);
    }

    [Fact]
    public void GreaterThanEqualsFalse_NullRowSurvives()
    {
        (TestDatabase db, (int Id, int? Value)[] seed) = SeedInts();
        using TestDatabase _ = db;

        List<int> expected = seed.Where(x => (x.Value > 5) == false).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NullableEntity>().Where(x => (x.Value > 5) == false).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GreaterThanEqualsTrue_NullRowExcluded()
    {
        (TestDatabase db, (int Id, int? Value)[] seed) = SeedInts();
        using TestDatabase _ = db;

        List<int> expected = seed.Where(x => (x.Value > 5) == true).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NullableEntity>().Where(x => (x.Value > 5) == true).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([2], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GreaterThanNotEqualsFalse_NullRowExcluded()
    {
        (TestDatabase db, (int Id, int? Value)[] seed) = SeedInts();
        using TestDatabase _ = db;

        List<int> expected = seed.Where(x => (x.Value > 5) != false).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NullableEntity>().Where(x => (x.Value > 5) != false).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([2], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GreaterThanNotEqualsTrue_NullRowSurvives()
    {
        (TestDatabase db, (int Id, int? Value)[] seed) = SeedInts();
        using TestDatabase _ = db;

        List<int> expected = seed.Where(x => (x.Value > 5) != true).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NullableEntity>().Where(x => (x.Value > 5) != true).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BoolConstantOnLeft_EqualsRelational_NullRowSurvives()
    {
        (TestDatabase db, (int Id, int? Value)[] seed) = SeedInts();
        using TestDatabase _ = db;

        List<int> expected = seed.Where(x => false == (x.Value > 5)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NullableEntity>().Where(x => false == (x.Value > 5)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LessThanOrEqualEqualsFalse_NullRowSurvives()
    {
        (TestDatabase db, (int Id, int? Value)[] seed) = SeedInts();
        using TestDatabase _ = db;

        List<int> expected = seed.Where(x => (x.Value <= 5) == false).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NullableEntity>().Where(x => (x.Value <= 5) == false).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 2], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RelationalEqualsRelational_BothNullableColumns_MatchesLinqToObjects()
    {
        TwoNullableIntEntity[] rows =
        [
            new TwoNullableIntEntity { Id = 1, A = null, B = null },
            new TwoNullableIntEntity { Id = 2, A = 10, B = 1 },
            new TwoNullableIntEntity { Id = 3, A = 10, B = 10 },
            new TwoNullableIntEntity { Id = 4, A = null, B = 10 },
        ];
        using TestDatabase db = new();
        db.Table<TwoNullableIntEntity>().Schema.CreateTable();
        foreach (TwoNullableIntEntity r in rows)
        {
            db.Table<TwoNullableIntEntity>().Add(r);
        }

        List<int> expected = rows.Where(x => (x.A > 5) == (x.B > 5)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableIntEntity>().Where(x => (x.A > 5) == (x.B > 5)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RelationalEqualsFalse_NonNullableColumn_StillCorrect()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "a", AuthorId = 3, Price = 1 },
            new Book { Id = 2, Title = "b", AuthorId = 10, Price = 2 },
        });

        Book[] seed =
        [
            new Book { Id = 1, Title = "a", AuthorId = 3, Price = 1 },
            new Book { Id = 2, Title = "b", AuthorId = 10, Price = 2 },
        ];

        List<int> expected = seed.Where(x => (x.AuthorId > 5) == false).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<Book>().Where(x => (x.AuthorId > 5) == false).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([1], expected);
        Assert.Equal(expected, actual);
    }
}
