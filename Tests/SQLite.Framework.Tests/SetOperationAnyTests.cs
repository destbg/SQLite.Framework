using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SetOperationAnyTests
{
    private static readonly int[] Values = [5, 3, 5, 1, 3];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 0; i < Values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, IntValue = Values[i] });
        }

        return db;
    }

    [Fact]
    public void IntersectAnyOverlappingMatchesInMemory()
    {
        using TestDatabase db = Seed();

        bool expected = Values.Intersect(Values.Where(v => v == 5)).Any();
        bool actual = db.Table<NumericType>().Select(x => x.IntValue)
            .Intersect(db.Table<NumericType>().Where(x => x.IntValue == 5).Select(x => x.IntValue))
            .Any();

        Assert.Equal(expected, actual);
        Assert.True(actual);
    }

    [Fact]
    public void IntersectAnyDisjointMatchesInMemory()
    {
        using TestDatabase db = Seed();

        bool expected = Values.Where(v => v == 1).Intersect(Values.Where(v => v == 3)).Any();
        bool actual = db.Table<NumericType>().Where(x => x.IntValue == 1).Select(x => x.IntValue)
            .Intersect(db.Table<NumericType>().Where(x => x.IntValue == 3).Select(x => x.IntValue))
            .Any();

        Assert.Equal(expected, actual);
        Assert.False(actual);
    }

    [Fact]
    public void ExceptAnyWithRemainderMatchesInMemory()
    {
        using TestDatabase db = Seed();

        bool expected = Values.Except(Values.Where(v => v == 5)).Any();
        bool actual = db.Table<NumericType>().Select(x => x.IntValue)
            .Except(db.Table<NumericType>().Where(x => x.IntValue == 5).Select(x => x.IntValue))
            .Any();

        Assert.Equal(expected, actual);
        Assert.True(actual);
    }

    [Fact]
    public void ExceptAnyFullyCoveredMatchesInMemory()
    {
        using TestDatabase db = Seed();

        bool expected = Values.Where(v => v == 5).Except(Values.Where(v => v == 5)).Any();
        bool actual = db.Table<NumericType>().Where(x => x.IntValue == 5).Select(x => x.IntValue)
            .Except(db.Table<NumericType>().Where(x => x.IntValue == 5).Select(x => x.IntValue))
            .Any();

        Assert.Equal(expected, actual);
        Assert.False(actual);
    }

    [Fact]
    public void UnionAnyNonEmptyMatchesInMemory()
    {
        using TestDatabase db = Seed();

        bool expected = Values.Where(v => v == 1).Union(Values.Where(v => v == 3)).Any();
        bool actual = db.Table<NumericType>().Where(x => x.IntValue == 1).Select(x => x.IntValue)
            .Union(db.Table<NumericType>().Where(x => x.IntValue == 3).Select(x => x.IntValue))
            .Any();

        Assert.Equal(expected, actual);
        Assert.True(actual);
    }

    [Fact]
    public void UnionAnyBothEmptyMatchesInMemory()
    {
        using TestDatabase db = Seed();

        bool expected = Values.Where(v => v == 99).Union(Values.Where(v => v == 99)).Any();
        bool actual = db.Table<NumericType>().Where(x => x.IntValue == 99).Select(x => x.IntValue)
            .Union(db.Table<NumericType>().Where(x => x.IntValue == 99).Select(x => x.IntValue))
            .Any();

        Assert.Equal(expected, actual);
        Assert.False(actual);
    }

    [Fact]
    public void ConcatAnyNonEmptyMatchesInMemory()
    {
        using TestDatabase db = Seed();

        bool expected = Values.Where(v => v == 1).Concat(Values.Where(v => v == 3)).Any();
        bool actual = db.Table<NumericType>().Where(x => x.IntValue == 1).Select(x => x.IntValue)
            .Concat(db.Table<NumericType>().Where(x => x.IntValue == 3).Select(x => x.IntValue))
            .Any();

        Assert.Equal(expected, actual);
        Assert.True(actual);
    }

    [Fact]
    public void ConcatAnyBothEmptyMatchesInMemory()
    {
        using TestDatabase db = Seed();

        bool expected = Values.Where(v => v == 99).Concat(Values.Where(v => v == 99)).Any();
        bool actual = db.Table<NumericType>().Where(x => x.IntValue == 99).Select(x => x.IntValue)
            .Concat(db.Table<NumericType>().Where(x => x.IntValue == 99).Select(x => x.IntValue))
            .Any();

        Assert.Equal(expected, actual);
        Assert.False(actual);
    }
}
