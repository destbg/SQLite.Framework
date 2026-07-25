using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H21hCastGrade
{
    Bravo = 1,
    Alpha = 2,
    Charlie = 3,
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<H21hCastGrade>))]
public partial class H21hCastGradeContext : JsonSerializerContext;

[Table("H21hCastGradeRows")]
public class H21hCastGradeRow
{
    [Key]
    public int Id { get; set; }

    public List<H21hCastGrade> Grades { get; set; } = [];
}

public class JsonStringEnumNumericCastAggregateParityTests
{
    private static List<H21hCastGrade> Grades()
    {
        return [H21hCastGrade.Bravo, H21hCastGrade.Alpha];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H21hCastGradeContext.Default), methodName);
        db.Table<H21hCastGradeRow>().Schema.CreateTable();
        db.Table<H21hCastGradeRow>().Add(new H21hCastGradeRow { Id = 1, Grades = Grades() });
        return db;
    }

    [Fact]
    public void SumOverCastStringStoredEnumElementMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(SumOverCastStringStoredEnumElementMatchesLinq));

        int expected = Grades().Sum(g => (int)g);
        int actual = db.Table<H21hCastGradeRow>().Select(r => r.Grades.Sum(g => (int)g)).First();

        Assert.Equal(3, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AverageOverCastStringStoredEnumElementMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(AverageOverCastStringStoredEnumElementMatchesLinq));

        double expected = Grades().Average(g => (int)g);
        double actual = db.Table<H21hCastGradeRow>().Select(r => r.Grades.Average(g => (int)g)).First();

        Assert.Equal(1.5, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountOfCastStringStoredEnumElementAboveThresholdMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(CountOfCastStringStoredEnumElementAboveThresholdMatchesLinq));

        int expected = Grades().Count(g => (int)g > 1);
        int actual = db.Table<H21hCastGradeRow>().Select(r => r.Grades.Count(g => (int)g > 1)).First();

        Assert.Equal(1, expected);
        Assert.Equal(expected, actual);
    }
}
