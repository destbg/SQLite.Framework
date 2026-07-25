using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("json3_inline_array")]
public sealed class Json3InlineArrayRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];

    public List<string> Words { get; set; } = [];
}

[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(List<int[]>))]
[JsonSerializable(typeof(List<int[][]>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<string[]>))]
internal partial class Json3InlineArrayContext : JsonSerializerContext;

public class JsonListSelectInlineArrayProjectionTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(Json3InlineArrayContext.Default.ListInt32);
            b.TypeConverters[typeof(List<int[]>)] =
                new SQLiteJsonConverter<List<int[]>>(Json3InlineArrayContext.Default.ListInt32Array);
            b.TypeConverters[typeof(List<int[][]>)] =
                new SQLiteJsonConverter<List<int[][]>>(Json3InlineArrayContext.Default.ListInt32ArrayArray);
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(Json3InlineArrayContext.Default.ListString);
            b.TypeConverters[typeof(List<string[]>)] =
                new SQLiteJsonConverter<List<string[]>>(Json3InlineArrayContext.Default.ListStringArray);
        });
        db.Table<Json3InlineArrayRow>().Schema.CreateTable();
        db.Table<Json3InlineArrayRow>().Add(new Json3InlineArrayRow
        {
            Id = 1,
            Numbers = [1, 2, 3],
            Words = ["a", "b"]
        });
        return db;
    }

    [Fact]
    public void SelectProjectingInlineArrayMatchesDotNet()
    {
        using TestDatabase db = CreateDb();

        List<int[]> expected = new List<int> { 1, 2, 3 }.Select(x => new[] { x, x * 2 }).ToList();
        List<int[]> actual = db.Table<Json3InlineArrayRow>()
            .Select(r => r.Numbers.Select(x => new[] { x, x * 2 }).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectProjectingInlineStringArrayMatchesDotNet()
    {
        using TestDatabase db = CreateDb();

        List<string[]> expected = new List<string> { "a", "b" }.Select(w => new[] { w, w + w }).ToList();
        List<string[]> actual = db.Table<Json3InlineArrayRow>()
            .Select(r => r.Words.Select(w => new[] { w, w + w }).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectManyProjectingInlineListMatchesDotNet()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = new List<int> { 1, 2, 3 }.SelectMany(x => new List<int> { x, x + 10 }).ToList();
        List<int> actual = db.Table<Json3InlineArrayRow>()
            .Select(r => r.Numbers.SelectMany(x => new List<int> { x, x + 10 }).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectProjectingEmptyInlineArrayMatchesDotNet()
    {
        using TestDatabase db = CreateDb();

        List<int[]> expected = new List<int> { 1, 2, 3 }.Select(x => new int[] { }).ToList();
        List<int[]> actual = db.Table<Json3InlineArrayRow>()
            .Select(r => r.Numbers.Select(x => new int[] { }).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectProjectingNestedInlineArrayMatchesDotNet()
    {
        using TestDatabase db = CreateDb();

        List<int[][]> expected = new List<int> { 1, 2, 3 }.Select(x => new[] { new[] { x } }).ToList();
        List<int[][]> actual = db.Table<Json3InlineArrayRow>()
            .Select(r => r.Numbers.Select(x => new[] { new[] { x } }).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectProjectingInlineArrayWithAMethodCallElementIsNotTranslated()
    {
        using TestDatabase db = CreateDb();

        Assert.Throws<NotSupportedException>(() => db.Table<Json3InlineArrayRow>()
            .Select(r => r.Numbers.Select(x => new[] { x, Scale(x) }).ToList())
            .First());
    }

    [Fact]
    public void SelectProjectingInlineDictionaryIsNotTranslated()
    {
        using TestDatabase db = CreateDb();

        Assert.Throws<NotSupportedException>(() => db.Table<Json3InlineArrayRow>()
            .Select(r => r.Numbers.Select(x => new Dictionary<int, int> { { x, x } }).ToList())
            .First());
    }

    private static int Scale(int value)
    {
        return value * 100;
    }
}
