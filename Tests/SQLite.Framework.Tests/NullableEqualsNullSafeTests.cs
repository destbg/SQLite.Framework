using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NullableEqualsNullSafeTests
{
    private static TestDatabase SeedInts(params (int id, int? value)[] rows)
    {
        TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        foreach ((int id, int? value) in rows)
        {
            db.Table<NullableEntity>().Add(new NullableEntity { Id = id, Value = value });
        }

        return db;
    }

    private static TestDatabase SeedPairs(params (int id, int? a, int? b)[] rows)
    {
        TestDatabase db = new();
        db.Table<TwoNullableIntEntity>().Schema.CreateTable();
        foreach ((int id, int? a, int? b) in rows)
        {
            db.Table<TwoNullableIntEntity>().Add(new TwoNullableIntEntity { Id = id, A = a, B = b });
        }

        return db;
    }

    [Fact]
    public void Equals_NullConstant_Projection_MatchesLinqToObjects()
    {
        (int id, int? value)[] rows = [(1, null), (2, 10)];
        using TestDatabase db = SeedInts(rows);

        int? other = null;
        List<bool> expected = rows.OrderBy(r => r.id).Select(r => r.value.Equals(other)).ToList();
        List<bool> actual = db.Table<NullableEntity>().OrderBy(x => x.Id).Select(x => x.Value.Equals(other)).ToList();

        Assert.Equal([true, false], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Equals_NonNullConstant_Projection_MatchesLinqToObjects()
    {
        (int id, int? value)[] rows = [(1, null), (2, 10), (3, 5)];
        using TestDatabase db = SeedInts(rows);

        int? other = 10;
        List<bool> expected = rows.OrderBy(r => r.id).Select(r => r.value.Equals(other)).ToList();
        List<bool> actual = db.Table<NullableEntity>().OrderBy(x => x.Id).Select(x => x.Value.Equals(other)).ToList();

        Assert.Equal([false, true, false], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Equals_ColumnToColumn_Where_MatchesLinqToObjects()
    {
        (int id, int? a, int? b)[] rows =
        [
            (1, null, null),
            (2, 5, 5),
            (3, 5, 7),
            (4, null, 5),
            (5, 5, null),
        ];
        using TestDatabase db = SeedPairs(rows);

        List<int> expected = rows.Where(p => p.a.Equals(p.b)).Select(p => p.id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableIntEntity>().Where(p => p.A.Equals(p.B)).Select(p => p.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 2], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Equals_NullConstant_Where_MatchesLinqToObjects()
    {
        (int id, int? value)[] rows = [(1, null), (2, 10), (3, null)];
        using TestDatabase db = SeedInts(rows);

        int? other = null;
        List<int> expected = rows.Where(r => r.value.Equals(other)).Select(r => r.id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NullableEntity>().Where(x => x.Value.Equals(other)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Equals_NonNullColumn_Where_StillMatchesAndExcludesNull()
    {
        (int id, int? value)[] rows = [(1, null), (2, 10), (3, 10), (4, 5)];
        using TestDatabase db = SeedInts(rows);

        List<int> expected = rows.Where(r => r.value.Equals(10)).Select(r => r.id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NullableEntity>().Where(x => x.Value.Equals(10)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([2, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Equals_NonNullStringColumn_Where_StillMatches()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Test", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "test", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "Other", AuthorId = 2, Price = 20 },
        });

        List<int> expected = new[] { 1 }.ToList();
        List<int> actual = db.Table<Book>().Where(b => b.Title.Equals("Test")).Select(b => b.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }
}
