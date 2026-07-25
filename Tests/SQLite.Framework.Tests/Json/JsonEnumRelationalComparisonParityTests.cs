using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum JerGrade
{
    Bravo = 1,
    Alpha = 2,
    Charlie = 3,
}

public enum JerLevel
{
    Low = 1,
    Mid = 2,
    High = 3,
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<JerGrade>))]
public partial class JerGradeContext : JsonSerializerContext;

[Table("JerGradeRows")]
public class JerGradeRow
{
    [Key]
    public int Id { get; set; }

    public List<JerGrade> Grades { get; set; } = [];
}

[Table("JerPlainRows")]
public class JerPlainRow
{
    [Key]
    public int Id { get; set; }

    public JerLevel Level { get; set; }
}

public class JsonEnumRelationalComparisonParityTests
{
    private static TestDatabase SeedGrades(out List<JerGradeRow> rows, string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(JerGradeContext.Default), methodName);
        db.Table<JerGradeRow>().Schema.CreateTable();
        rows =
        [
            new JerGradeRow { Id = 1, Grades = [JerGrade.Bravo, JerGrade.Alpha] },
            new JerGradeRow { Id = 2, Grades = [JerGrade.Charlie, JerGrade.Bravo] },
            new JerGradeRow { Id = 3, Grades = [] },
        ];
        db.Table<JerGradeRow>().AddRange(rows);
        return db;
    }

    [Fact]
    public void StringStoredEnumConstantOnLeftMatchesLinq()
    {
        using TestDatabase db = SeedGrades(out List<JerGradeRow> rows, nameof(StringStoredEnumConstantOnLeftMatchesLinq));

        List<List<JerGrade>> expected = rows.Select(r => r.Grades.Where(g => JerGrade.Bravo < g).ToList()).ToList();
        List<List<JerGrade>> actual = db.Table<JerGradeRow>().Select(r => r.Grades.Where(g => JerGrade.Bravo < g).ToList()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PlainEnumColumnGreaterThanConstantMatchesLinq()
    {
        using TestDatabase db = new();
        db.Table<JerPlainRow>().Schema.CreateTable();
        List<JerPlainRow> rows =
        [
            new JerPlainRow { Id = 1, Level = JerLevel.Low },
            new JerPlainRow { Id = 2, Level = JerLevel.Mid },
            new JerPlainRow { Id = 3, Level = JerLevel.High },
        ];
        db.Table<JerPlainRow>().AddRange(rows);

        List<int> expected = rows.Where(r => r.Level > JerLevel.Low).Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<JerPlainRow>().Where(r => r.Level > JerLevel.Low).Select(r => r.Id).OrderBy(id => id).ToList();

        Assert.Equal(expected, actual);
    }
}
