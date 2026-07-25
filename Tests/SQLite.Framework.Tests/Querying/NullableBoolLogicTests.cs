using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NullableBoolLogicTests
{
    [Fact]
    public void OrWithLiftedGreaterThan()
    {
        Run(q => q.Select(r => r.FlagA | (r.NA > 3)));
    }

    [Fact]
    public void AndWithLiftedLessThan()
    {
        Run(q => q.Select(r => r.FlagA & (r.NA < 3)));
    }

    [Fact]
    public void AndWithLiftedGreaterOrEqual()
    {
        Run(q => q.Select(r => r.FlagA & (r.NA >= 5)));
    }

    [Fact]
    public void OrWithLiftedLessOrEqual()
    {
        Run(q => q.Select(r => r.FlagA | (r.NB <= 4)));
    }

    [Fact]
    public void ChainedAndTwoComparisons()
    {
        Run(q => q.Select(r => r.FlagA & (r.NA > 3) & (r.NB < 10)));
    }

    [Fact]
    public void NestedOrInsideAnd()
    {
        Run(q => q.Select(r => r.FlagA & ((r.NA > 3) | (r.NB > 3))));
    }

    [Fact]
    public void NestedAndInsideOr()
    {
        Run(q => q.Select(r => r.FlagA | ((r.NA > 3) & (r.NB > 3))));
    }

    [Fact]
    public void BothOperandsLiftedComparisons()
    {
        Run(q => q.Select(r => (r.NA > 3) & (r.NB < 10)));
    }

    [Fact]
    public void PlainNullableBoolAnd()
    {
        Run(q => q.Select(r => r.FlagA & r.FlagB));
    }

    [Fact]
    public void PlainNullableBoolOr()
    {
        Run(q => q.Select(r => r.FlagA | r.FlagB));
    }

    [Fact]
    public void NonNullableComparisonsAnd()
    {
        Run(q => q.Select(r => (r.X > 0) & (r.Y < 5)));
    }

    [Fact]
    public void EqualityOperandUnderAnd()
    {
        Run(q => q.Select(r => r.FlagA & (r.NA == 5)));
    }

    [Fact]
    public void NotEqualOperandUnderOr()
    {
        Run(q => q.Select(r => r.FlagA | (r.NA != 5)));
    }

    [Fact]
    public void NonNullableXorWithLiftedComparisons()
    {
        Run(q => q.Select(r => (r.X > 0) ^ (r.NA > 3)));
    }

    [Fact]
    public void NullableXorWithLiftedComparison()
    {
        Run(q => q.Select(r => r.FlagA ^ (r.NA > 3)));
    }

    [Fact]
    public void CoalesceOverAndOfComparison()
    {
        Run(q => q.Select(r => (r.FlagA & (r.NA > 3)) ?? false));
    }

    [Fact]
    public void WhereTwoLiftedComparisonsAnded()
    {
        RunWhere(q => q.Where(r => r.NA > 3 && r.NB < 10));
    }

    [Fact]
    public void WhereOrPredicateWithLiftedComparison()
    {
        RunWhere(q => q.Where(r => (r.FlagA ?? false) || r.NA > 3));
    }

    [Fact]
    public void WhereAndAlsoNonNullableAndLifted()
    {
        RunWhere(q => q.Where(r => r.X > 0 && r.NA >= 5));
    }

    private static void Run<T>(Func<IQueryable<NbLogicRow>, IQueryable<T>> project)
    {
        using TestDatabase db = new();
        db.Table<NbLogicRow>().Schema.CreateTable();
        db.Table<NbLogicRow>().AddRange(Data());

        List<T> oracle = project(Data().AsQueryable().OrderBy(r => r.Id)).ToList();
        List<T> actual = project(db.Table<NbLogicRow>().OrderBy(r => r.Id)).ToList();

        Assert.Equal(oracle, actual);
    }

    private static void RunWhere(Func<IQueryable<NbLogicRow>, IQueryable<NbLogicRow>> filter)
    {
        using TestDatabase db = new();
        db.Table<NbLogicRow>().Schema.CreateTable();
        db.Table<NbLogicRow>().AddRange(Data());

        List<int> oracle = filter(Data().AsQueryable()).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = filter(db.Table<NbLogicRow>()).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    private static List<NbLogicRow> Data()
    {
        return new List<NbLogicRow>
        {
            new() { Id = 1, FlagA = null, FlagB = null, NA = null, NB = null, X = 0, Y = 0 },
            new() { Id = 2, FlagA = true, FlagB = false, NA = 7, NB = 2, X = 5, Y = 1 },
            new() { Id = 3, FlagA = false, FlagB = true, NA = 2, NB = 20, X = -2, Y = 9 },
            new() { Id = 4, FlagA = true, FlagB = null, NA = null, NB = 5, X = 3, Y = 4 },
            new() { Id = 5, FlagA = null, FlagB = true, NA = 5, NB = null, X = 8, Y = 0 },
            new() { Id = 6, FlagA = false, FlagB = false, NA = null, NB = 8, X = 1, Y = 7 },
            new() { Id = 7, FlagA = true, FlagB = true, NA = 10, NB = 1, X = 0, Y = 0 },
            new() { Id = 8, FlagA = null, FlagB = false, NA = 4, NB = 4, X = 2, Y = 2 },
        };
    }
}

public class NbLogicRow
{
    [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; }
    public bool? FlagA { get; set; }
    public bool? FlagB { get; set; }
    public int? NA { get; set; }
    public int? NB { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}
