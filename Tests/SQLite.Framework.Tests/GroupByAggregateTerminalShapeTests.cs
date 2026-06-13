using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class GroupedTerminalRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Value { get; set; }
}

public class GroupByAggregateTerminalShapeTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<GroupedTerminalRow>().Schema.CreateTable();
        db.Table<GroupedTerminalRow>().Add(new GroupedTerminalRow { Id = 1, Name = "a", Value = 10 });
        db.Table<GroupedTerminalRow>().Add(new GroupedTerminalRow { Id = 2, Name = "a", Value = 20 });
        db.Table<GroupedTerminalRow>().Add(new GroupedTerminalRow { Id = 3, Name = "b", Value = 5 });
        return db;
    }

    [Fact]
    public void AnyWithGroupCountPredicate()
    {
        using TestDatabase db = SetupDatabase();

        bool expected = db.Table<GroupedTerminalRow>().AsEnumerable()
            .GroupBy(r => r.Name)
            .Any(g => g.Count() > 1);

        Assert.True(expected);

        bool actual = db.Table<GroupedTerminalRow>()
            .GroupBy(r => r.Name)
            .Any(g => g.Count() > 1);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AllWithGroupCountPredicate()
    {
        using TestDatabase db = SetupDatabase();

        bool expected = db.Table<GroupedTerminalRow>().AsEnumerable()
            .GroupBy(r => r.Name)
            .All(g => g.Count() > 1);

        Assert.False(expected);

        bool actual = db.Table<GroupedTerminalRow>()
            .GroupBy(r => r.Name)
            .All(g => g.Count() > 1);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxOfGroupCounts()
    {
        using TestDatabase db = SetupDatabase();

        int expected = db.Table<GroupedTerminalRow>().AsEnumerable()
            .GroupBy(r => r.Name)
            .Max(g => g.Count());

        Assert.Equal(2, expected);

        int actual = db.Table<GroupedTerminalRow>()
            .GroupBy(r => r.Name)
            .Max(g => g.Count());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumOfGroupCounts()
    {
        using TestDatabase db = SetupDatabase();

        int expected = db.Table<GroupedTerminalRow>().AsEnumerable()
            .GroupBy(r => r.Name)
            .Select(g => g.Count())
            .Sum();

        Assert.Equal(3, expected);

        int actual = db.Table<GroupedTerminalRow>()
            .GroupBy(r => r.Name)
            .Select(g => g.Count())
            .Sum();

        Assert.Equal(expected, actual);
    }

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void FirstWithGroupCountPredicateReturnsTheKey()
    {
        using TestDatabase db = SetupDatabase();

        string expected = db.Table<GroupedTerminalRow>().AsEnumerable()
            .GroupBy(r => r.Name)
            .First(g => g.Count() > 1)
            .Key;

        Assert.Equal("a", expected);

        string actual = db.Table<GroupedTerminalRow>()
            .GroupBy(r => r.Name)
            .First(g => g.Count() > 1)
            .Key;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstWithoutPredicateReturnsFirstGroupKey()
    {
        using TestDatabase db = SetupDatabase();

        string expected = db.Table<GroupedTerminalRow>().AsEnumerable()
            .GroupBy(r => r.Name)
            .First()
            .Key;

        string actual = db.Table<GroupedTerminalRow>()
            .GroupBy(r => r.Name)
            .First()
            .Key;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstOrDefaultWithPredicateReturnsMatchKey()
    {
        using TestDatabase db = SetupDatabase();

        string? expected = db.Table<GroupedTerminalRow>().AsEnumerable()
            .GroupBy(r => r.Name)
            .FirstOrDefault(g => g.Count() > 1)
            ?.Key;

        string? actual = db.Table<GroupedTerminalRow>()
            .GroupBy(r => r.Name)
            .FirstOrDefault(g => g.Count() > 1)
            ?.Key;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstOrDefaultWithoutPredicateReturnsFirstGroupKey()
    {
        using TestDatabase db = SetupDatabase();

        string? expected = db.Table<GroupedTerminalRow>().AsEnumerable()
            .GroupBy(r => r.Name)
            .FirstOrDefault()
            ?.Key;

        string? actual = db.Table<GroupedTerminalRow>()
            .GroupBy(r => r.Name)
            .FirstOrDefault()
            ?.Key;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstOrDefaultWithPredicateNoMatchReturnsNull()
    {
        using TestDatabase db = SetupDatabase();

        IGrouping<string, GroupedTerminalRow>? expected = db.Table<GroupedTerminalRow>().AsEnumerable()
            .GroupBy(r => r.Name)
            .FirstOrDefault(g => g.Count() > 5);

        Assert.Null(expected);

        IGrouping<string, GroupedTerminalRow>? actual = db.Table<GroupedTerminalRow>()
            .GroupBy(r => r.Name)
            .FirstOrDefault(g => g.Count() > 5);

        Assert.Null(actual);
    }

    [Fact]
    public void SingleWithPredicateReturnsMatchKey()
    {
        using TestDatabase db = SetupDatabase();

        string expected = db.Table<GroupedTerminalRow>().AsEnumerable()
            .GroupBy(r => r.Name)
            .Single(g => g.Count() > 1)
            .Key;

        string actual = db.Table<GroupedTerminalRow>()
            .GroupBy(r => r.Name)
            .Single(g => g.Count() > 1)
            .Key;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SingleWithoutPredicateReturnsOnlyGroupKey()
    {
        using TestDatabase db = SetupSingleGroupDatabase();

        string expected = db.Table<GroupedTerminalRow>().AsEnumerable()
            .GroupBy(r => r.Name)
            .Single()
            .Key;

        string actual = db.Table<GroupedTerminalRow>()
            .GroupBy(r => r.Name)
            .Single()
            .Key;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SingleOrDefaultWithPredicateReturnsMatchKey()
    {
        using TestDatabase db = SetupDatabase();

        string? expected = db.Table<GroupedTerminalRow>().AsEnumerable()
            .GroupBy(r => r.Name)
            .SingleOrDefault(g => g.Count() > 1)
            ?.Key;

        string? actual = db.Table<GroupedTerminalRow>()
            .GroupBy(r => r.Name)
            .SingleOrDefault(g => g.Count() > 1)
            ?.Key;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SingleOrDefaultWithoutPredicateReturnsOnlyGroupKey()
    {
        using TestDatabase db = SetupSingleGroupDatabase();

        string? expected = db.Table<GroupedTerminalRow>().AsEnumerable()
            .GroupBy(r => r.Name)
            .SingleOrDefault()
            ?.Key;

        string? actual = db.Table<GroupedTerminalRow>()
            .GroupBy(r => r.Name)
            .SingleOrDefault()
            ?.Key;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstOverWrappedGroupingIsNotSupported()
    {
        using TestDatabase db = SetupDatabase();

        Assert.ThrowsAny<Exception>(() =>
            db.Table<GroupedTerminalRow>()
                .GroupBy(r => r.Name)
                .Where(g => g.Count() > 0)
                .First());
    }

    private static TestDatabase SetupSingleGroupDatabase()
    {
        TestDatabase db = new();
        db.Table<GroupedTerminalRow>().Schema.CreateTable();
        db.Table<GroupedTerminalRow>().Add(new GroupedTerminalRow { Id = 1, Name = "a", Value = 10 });
        db.Table<GroupedTerminalRow>().Add(new GroupedTerminalRow { Id = 2, Name = "a", Value = 20 });
        return db;
    }
#endif
}
