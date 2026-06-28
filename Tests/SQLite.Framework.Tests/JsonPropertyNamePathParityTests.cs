using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class DottedNameItem
{
    [JsonPropertyName("first.last")] public string Name { get; set; } = "";
}

internal sealed class QuotedNameDoc
{
    [JsonPropertyName("it's")] public string Field { get; set; } = "";
}

internal sealed class DottedNameChild
{
    public int Score { get; set; }
}

internal sealed class DottedNameParent
{
    [JsonPropertyName("par.ent")] public DottedNameChild Inner { get; set; } = new();
}

[JsonSerializable(typeof(List<DottedNameItem>))]
[JsonSerializable(typeof(QuotedNameDoc))]
[JsonSerializable(typeof(List<DottedNameParent>))]
internal partial class JsonPropertyNamePathContext : JsonSerializerContext;

internal sealed class DottedNameItemRow
{
    [Key] public int Id { get; set; }
    public List<DottedNameItem> Items { get; set; } = [];
}

internal sealed class QuotedNameRow
{
    [Key] public int Id { get; set; }
    public QuotedNameDoc D { get; set; } = new();
}

internal sealed class DottedNameParentRow
{
    [Key] public int Id { get; set; }
    public List<DottedNameParent> Items { get; set; } = [];
}

public class JsonPropertyNamePathParityTests
{
    [Fact]
    public void JsonPropertyNameWithDot_ReadsCorrectValue()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<DottedNameItem>)] = new SQLiteJsonConverter<List<DottedNameItem>>(JsonPropertyNamePathContext.Default.ListDottedNameItem));
        db.Table<DottedNameItemRow>().Schema.CreateTable();
        List<DottedNameItemRow> rows = new() { new() { Id = 1, Items = [new DottedNameItem { Name = "hi" }] } };
        db.Table<DottedNameItemRow>().AddRange(rows);

        List<int> expected = rows.Where(r => r.Items.Any(i => i.Name == "hi")).Select(r => r.Id).OrderBy(x => x).ToList();
        List<int> actual = db.Table<DottedNameItemRow>().Where(r => r.Items.Any(i => i.Name == "hi")).Select(r => r.Id).OrderBy(x => x).ToList();
        Assert.Equal(expected, actual);
    }

#if !SQLITECIPHER
    [Fact]
    public void JsonPropertyNameWithSingleQuote_ReadsCorrectValue()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(QuotedNameDoc)] = new SQLiteJsonConverter<QuotedNameDoc>(JsonPropertyNamePathContext.Default.QuotedNameDoc));
        db.Table<QuotedNameRow>().Schema.CreateTable();
        List<QuotedNameRow> rows = new() { new() { Id = 1, D = new QuotedNameDoc { Field = "x" } } };
        db.Table<QuotedNameRow>().AddRange(rows);

        List<int> expected = rows.Where(r => r.D.Field == "x").Select(r => r.Id).OrderBy(x => x).ToList();
        List<int> actual = db.Table<QuotedNameRow>().Where(r => r.D.Field == "x").Select(r => r.Id).OrderBy(x => x).ToList();
        Assert.Equal(expected, actual);
    }
#endif

    [Fact]
    public void NestedJsonPropertyNameWithDot_ReadsCorrectValue()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<DottedNameParent>)] = new SQLiteJsonConverter<List<DottedNameParent>>(JsonPropertyNamePathContext.Default.ListDottedNameParent));
        db.Table<DottedNameParentRow>().Schema.CreateTable();
        List<DottedNameParentRow> rows = new() { new() { Id = 1, Items = [new DottedNameParent { Inner = new DottedNameChild { Score = 5 } }] } };
        db.Table<DottedNameParentRow>().AddRange(rows);

        List<int> expected = rows.Where(r => r.Items.Any(p => p.Inner.Score == 5)).Select(r => r.Id).OrderBy(x => x).ToList();
        List<int> actual = db.Table<DottedNameParentRow>().Where(r => r.Items.Any(p => p.Inner.Score == 5)).Select(r => r.Id).OrderBy(x => x).ToList();
        Assert.Equal(expected, actual);
    }
}
