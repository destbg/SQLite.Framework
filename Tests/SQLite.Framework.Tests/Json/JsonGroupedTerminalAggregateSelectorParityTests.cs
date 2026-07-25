using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H21hAggGrade
{
    Bravo = 1,
    Alpha = 2,
    Charlie = 3,
}

[JsonSerializable(typeof(List<int>))]
public partial class H21hGroupedAggContext : JsonSerializerContext;

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<H21hAggGrade>))]
public partial class H21hAggGradeContext : JsonSerializerContext;

[Table("H21hGroupedAggRows")]
public class H21hGroupedAggRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Nums { get; set; } = [];
}

[Table("H21hGroupedAggGradeRows")]
public class H21hGroupedAggGradeRow
{
    [Key]
    public int Id { get; set; }

    public List<H21hAggGrade> Grades { get; set; } = [];
}

public class JsonGroupedTerminalAggregateSelectorParityTests
{
    private static List<int> Numbers()
    {
        return [1, 1, 1, 4];
    }

    private static List<H21hAggGrade> Grades()
    {
        return [H21hAggGrade.Charlie, H21hAggGrade.Bravo, H21hAggGrade.Alpha];
    }

    private static TestDatabase SetupNumbers(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H21hGroupedAggContext.Default), methodName);
        db.Table<H21hGroupedAggRow>().Schema.CreateTable();
        db.Table<H21hGroupedAggRow>().Add(new H21hGroupedAggRow { Id = 1, Nums = Numbers() });
        return db;
    }

    private static TestDatabase SetupGrades(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H21hAggGradeContext.Default), methodName);
        db.Table<H21hGroupedAggGradeRow>().Schema.CreateTable();
        db.Table<H21hGroupedAggGradeRow>().Add(new H21hGroupedAggGradeRow { Id = 1, Grades = Grades() });
        return db;
    }

    [Fact]
    public void GroupedSumOverGroupKeyMatchesLinq()
    {
        using TestDatabase db = SetupNumbers(nameof(GroupedSumOverGroupKeyMatchesLinq));

        int expected = Numbers().GroupBy(n => n).Sum(g => g.Key);
        int actual = db.Table<H21hGroupedAggRow>().Select(r => r.Nums.GroupBy(n => n).Sum(g => g.Key)).First();

        Assert.Equal(5, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupedAverageOverGroupKeyMatchesLinq()
    {
        using TestDatabase db = SetupNumbers(nameof(GroupedAverageOverGroupKeyMatchesLinq));

        double expected = Numbers().GroupBy(n => n).Average(g => g.Key);
        double actual = db.Table<H21hGroupedAggRow>().Select(r => r.Nums.GroupBy(n => n).Average(g => g.Key)).First();

        Assert.Equal(2.5, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupedMaxOverStringStoredEnumGroupKeyMatchesLinq()
    {
        using TestDatabase db = SetupGrades(nameof(GroupedMaxOverStringStoredEnumGroupKeyMatchesLinq));

        H21hAggGrade expected = Grades().GroupBy(g => g).Max(x => x.Key);
        H21hAggGrade actual = db.Table<H21hGroupedAggGradeRow>()
            .Select(r => r.Grades.GroupBy(g => g).Max(x => x.Key)).First();

        Assert.Equal(H21hAggGrade.Charlie, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupedMinOverStringStoredEnumGroupKeyMatchesLinq()
    {
        using TestDatabase db = SetupGrades(nameof(GroupedMinOverStringStoredEnumGroupKeyMatchesLinq));

        H21hAggGrade expected = Grades().GroupBy(g => g).Min(x => x.Key);
        H21hAggGrade actual = db.Table<H21hGroupedAggGradeRow>()
            .Select(r => r.Grades.GroupBy(g => g).Min(x => x.Key)).First();

        Assert.Equal(H21hAggGrade.Bravo, expected);
        Assert.Equal(expected, actual);
    }
}
