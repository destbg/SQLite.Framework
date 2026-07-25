using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H20JsonGrade
{
    Bravo = 1,
    Alpha = 2,
    Charlie = 3,
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<H20JsonGrade>))]
[JsonSerializable(typeof(List<H20JsonGrade?>))]
public partial class H20JsonGradeContext : JsonSerializerContext;

[Table("H20JsonGradeRows")]
public class H20JsonGradeRow
{
    [Key]
    public int Id { get; set; }

    public List<H20JsonGrade> Grades { get; set; } = [];

    public List<H20JsonGrade?> MaybeGrades { get; set; } = [];
}

public class JsonStringEnumNumericSemanticsTests
{
    private static TestDatabase Seed(out List<H20JsonGradeRow> rows, string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H20JsonGradeContext.Default), methodName);
        db.Table<H20JsonGradeRow>().Schema.CreateTable();
        rows =
        [
            new H20JsonGradeRow
            {
                Id = 1,
                Grades = [H20JsonGrade.Bravo, H20JsonGrade.Alpha],
                MaybeGrades = [H20JsonGrade.Bravo, H20JsonGrade.Alpha],
            },
            new H20JsonGradeRow
            {
                Id = 2,
                Grades = [H20JsonGrade.Charlie, H20JsonGrade.Bravo],
                MaybeGrades = [H20JsonGrade.Charlie, H20JsonGrade.Bravo],
            },
        ];
        db.Table<H20JsonGradeRow>().AddRange(rows);
        return db;
    }

    [Fact]
    public void MinAfterSelectOverStringEnumListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonGradeRow> rows, nameof(MinAfterSelectOverStringEnumListMatchesLinq));

        List<H20JsonGrade> expected = rows.Select(r => r.Grades.Select(g => g).Min()).ToList();
        List<H20JsonGrade> actual = db.Table<H20JsonGradeRow>().Select(r => r.Grades.Select(g => g).Min()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxWithIdentitySelectorOverStringEnumListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonGradeRow> rows, nameof(MaxWithIdentitySelectorOverStringEnumListMatchesLinq));

        List<H20JsonGrade> expected = rows.Select(r => r.Grades.Max(g => g)).ToList();
        List<H20JsonGrade> actual = db.Table<H20JsonGradeRow>().Select(r => r.Grades.Max(g => g)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MinOverNullableStringEnumListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonGradeRow> rows, nameof(MinOverNullableStringEnumListMatchesLinq));

        List<H20JsonGrade?> expected = rows.Select(r => r.MaybeGrades.Min()).ToList();
        List<H20JsonGrade?> actual = db.Table<H20JsonGradeRow>().Select(r => r.MaybeGrades.Min()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxOverNullableStringEnumListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonGradeRow> rows, nameof(MaxOverNullableStringEnumListMatchesLinq));

        List<H20JsonGrade?> expected = rows.Select(r => r.MaybeGrades.Max()).ToList();
        List<H20JsonGrade?> actual = db.Table<H20JsonGradeRow>().Select(r => r.MaybeGrades.Max()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MinWithIdentitySelectorOverNullableStringEnumListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonGradeRow> rows, nameof(MinWithIdentitySelectorOverNullableStringEnumListMatchesLinq));

        List<H20JsonGrade?> expected = rows.Select(r => r.MaybeGrades.Min(g => g)).ToList();
        List<H20JsonGrade?> actual = db.Table<H20JsonGradeRow>().Select(r => r.MaybeGrades.Min(g => g)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxWithIdentitySelectorOverNullableStringEnumListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonGradeRow> rows, nameof(MaxWithIdentitySelectorOverNullableStringEnumListMatchesLinq));

        List<H20JsonGrade?> expected = rows.Select(r => r.MaybeGrades.Max(g => g)).ToList();
        List<H20JsonGrade?> actual = db.Table<H20JsonGradeRow>().Select(r => r.MaybeGrades.Max(g => g)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereGreaterThanOverStringEnumListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H20JsonGradeRow> rows, nameof(WhereGreaterThanOverStringEnumListMatchesLinq));

        List<List<H20JsonGrade>> expected = rows.Select(r => r.Grades.Where(g => g > H20JsonGrade.Bravo).ToList()).ToList();
        List<List<H20JsonGrade>> actual = db.Table<H20JsonGradeRow>().Select(r => r.Grades.Where(g => g > H20JsonGrade.Bravo).ToList()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UndefinedNumericValueMinMaxMatchesLinq()
    {
        using TestDatabase db = new(b => b.AddJsonContext(H20JsonGradeContext.Default), nameof(UndefinedNumericValueMinMaxMatchesLinq));
        db.Table<H20JsonGradeRow>().Schema.CreateTable();
        List<H20JsonGradeRow> rows =
        [
            new H20JsonGradeRow { Id = 1, Grades = [H20JsonGrade.Bravo, (H20JsonGrade)5] },
        ];
        db.Table<H20JsonGradeRow>().AddRange(rows);

        List<H20JsonGrade> expectedMin = rows.Select(r => r.Grades.Min()).ToList();
        List<H20JsonGrade> expectedMax = rows.Select(r => r.Grades.Max()).ToList();
        List<H20JsonGrade> actualMin = db.Table<H20JsonGradeRow>().Select(r => r.Grades.Min()).ToList();
        List<H20JsonGrade> actualMax = db.Table<H20JsonGradeRow>().Select(r => r.Grades.Max()).ToList();

        Assert.Equal(expectedMin, actualMin);
        Assert.Equal(expectedMax, actualMax);
    }
}
