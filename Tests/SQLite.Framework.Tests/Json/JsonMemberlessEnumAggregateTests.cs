using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum BlankJsonMode
{
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<BlankJsonMode>))]
public partial class BlankJsonModeContext : JsonSerializerContext;

[Table("BlankJsonModeRows")]
public class BlankJsonModeRow
{
    [Key]
    public int Id { get; set; }

    public List<BlankJsonMode> Modes { get; set; } = [];
}

public class JsonMemberlessEnumAggregateTests
{
    [Fact]
    public void MinOverAMemberlessEnumListMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(MinOverAMemberlessEnumListMatchesLinq));

        List<BlankJsonMode> expected = Rows().OrderBy(r => r.Id).Select(r => r.Modes.Select(m => m).Min()).ToList();
        List<BlankJsonMode> actual = db.Table<BlankJsonModeRow>().OrderBy(r => r.Id)
            .Select(r => r.Modes.Select(m => m).Min()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxOverAMemberlessEnumListMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(MaxOverAMemberlessEnumListMatchesLinq));

        List<BlankJsonMode> expected = Rows().OrderBy(r => r.Id).Select(r => r.Modes.Select(m => m).Max()).ToList();
        List<BlankJsonMode> actual = db.Table<BlankJsonModeRow>().OrderBy(r => r.Id)
            .Select(r => r.Modes.Select(m => m).Max()).ToList();

        Assert.Equal(expected, actual);
    }

    private static List<BlankJsonModeRow> Rows()
    {
        return
        [
            new BlankJsonModeRow { Id = 1, Modes = [(BlankJsonMode)2, (BlankJsonMode)5] },
            new BlankJsonModeRow { Id = 2, Modes = [(BlankJsonMode)7] }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(BlankJsonModeContext.Default), methodName);
        db.Table<BlankJsonModeRow>().Schema.CreateTable();
        db.Table<BlankJsonModeRow>().AddRange(Rows());
        return db;
    }
}
