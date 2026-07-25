using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BooleanOperatorPrecedenceTests
{
    private static readonly (int id, int? a, int? b)[] Rows =
    {
        (1, null, null),
        (2, 5, 5),
        (3, 5, 7),
        (4, null, 5),
    };

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<TwoNullableIntEntity>().Schema.CreateTable();
        foreach ((int id, int? a, int? b) in Rows)
        {
            db.Table<TwoNullableIntEntity>().Add(new TwoNullableIntEntity { Id = id, A = a, B = b });
        }

        return db;
    }

    private static List<TwoNullableIntEntity> InMemory()
    {
        return Rows.Select(r => new TwoNullableIntEntity { Id = r.id, A = r.a, B = r.b }).ToList();
    }

    [Fact]
    public void XorOfTwoEqualityComparisonsMatchesInMemory()
    {
        using TestDatabase db = Seed();

        List<int> actual = db.Table<TwoNullableIntEntity>()
            .Where(x => (x.A == 5) ^ (x.B == 5))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> oracle = InMemory()
            .Where(x => (x.A == 5) ^ (x.B == 5))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void XorOfTwoIdEqualityComparisonsMatchesInMemory()
    {
        using TestDatabase db = Seed();

        List<int> actual = db.Table<TwoNullableIntEntity>()
            .Where(x => (x.Id == 3) ^ (x.Id == 4))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> oracle = InMemory()
            .Where(x => (x.Id == 3) ^ (x.Id == 4))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void EqualityOfTwoEqualityComparisonsMatchesInMemory()
    {
        using TestDatabase db = Seed();

        List<int> actual = db.Table<TwoNullableIntEntity>()
            .Where(x => (x.A == 5) == (x.B == 5))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> oracle = InMemory()
            .Where(x => (x.A == 5) == (x.B == 5))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void InequalityOfTwoIdEqualityComparisonsMatchesInMemory()
    {
        using TestDatabase db = Seed();

        List<int> actual = db.Table<TwoNullableIntEntity>()
            .Where(x => (x.Id == 3) != (x.Id == 4))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> oracle = InMemory()
            .Where(x => (x.Id == 3) != (x.Id == 4))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void AndOperandUnderXorMatchesInMemory()
    {
        using TestDatabase db = Seed();

        List<int> actual = db.Table<TwoNullableIntEntity>()
            .Where(x => ((x.A == 5) && (x.B == 5)) ^ (x.Id == 1))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> oracle = InMemory()
            .Where(x => ((x.A == 5) && (x.B == 5)) ^ (x.Id == 1))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void XorOperandUnderEqualityMatchesInMemory()
    {
        using TestDatabase db = Seed();

        List<int> actual = db.Table<TwoNullableIntEntity>()
            .Where(x => ((x.A == 5) ^ (x.B == 5)) == (x.Id == 2))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> oracle = InMemory()
            .Where(x => ((x.A == 5) ^ (x.B == 5)) == (x.Id == 2))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TripleNestedEqualityMatchesInMemory()
    {
        using TestDatabase db = Seed();

        List<int> actual = db.Table<TwoNullableIntEntity>()
            .Where(x => ((x.A == 5) == (x.B == 5)) == (x.Id == 2))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> oracle = InMemory()
            .Where(x => ((x.A == 5) == (x.B == 5)) == (x.Id == 2))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
