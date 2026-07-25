using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("j19dc_complex_rows")]
public sealed class Json19dcComplexRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];

    public Json19dcMeta Meta { get; set; } = new();

    public List<Json19dcItem> Items { get; set; } = [];
}

public sealed class Json19dcMeta
{
    public string Label { get; set; } = "";

    public int Rank { get; set; }
}

public sealed class Json19dcItem
{
    public string Name { get; set; } = "";

    public int Qty { get; set; }
}

[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(Json19dcMeta))]
[JsonSerializable(typeof(Json19dcItem))]
[JsonSerializable(typeof(List<Json19dcItem>))]
[JsonSerializable(typeof(List<Json19dcItem[]>))]
[JsonSerializable(typeof(List<Json19dcMeta[]>))]
[JsonSerializable(typeof(List<int>[]), TypeInfoPropertyName = "IntListArrays")]
[JsonSerializable(typeof(List<List<int>[]>), TypeInfoPropertyName = "NestedIntListArrays")]
internal partial class Json19dcComplexContext : JsonSerializerContext;

public class JsonInlineArrayComplexElementTests
{
    private static List<int> SrcNumbers => [1, 2, 3];

    private static List<Json19dcItem> SrcItems =>
    [
        new Json19dcItem { Name = "n1", Qty = 5 },
        new Json19dcItem { Name = "n2", Qty = 7 }
    ];

    private static TestDatabase CreateDbWithoutItemConverter()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(Json19dcComplexContext.Default.ListInt32);
            b.TypeConverters[typeof(Json19dcMeta)] =
                new SQLiteJsonConverter<Json19dcMeta>(Json19dcComplexContext.Default.Json19dcMeta);
            b.TypeConverters[typeof(List<Json19dcItem>)] =
                new SQLiteJsonConverter<List<Json19dcItem>>(Json19dcComplexContext.Default.ListJson19dcItem);
            b.TypeConverters[typeof(List<Json19dcItem[]>)] =
                new SQLiteJsonConverter<List<Json19dcItem[]>>(Json19dcComplexContext.Default.ListJson19dcItemArray);
        });
        db.Table<Json19dcComplexRow>().Schema.CreateTable();
        db.Table<Json19dcComplexRow>().Add(new Json19dcComplexRow
        {
            Id = 1,
            Numbers = SrcNumbers,
            Meta = new Json19dcMeta { Label = "m1", Rank = 4 },
            Items = SrcItems
        });
        return db;
    }

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(Json19dcComplexContext.Default.ListInt32);
            b.TypeConverters[typeof(Json19dcMeta)] =
                new SQLiteJsonConverter<Json19dcMeta>(Json19dcComplexContext.Default.Json19dcMeta);
            b.TypeConverters[typeof(Json19dcItem)] =
                new SQLiteJsonConverter<Json19dcItem>(Json19dcComplexContext.Default.Json19dcItem);
            b.TypeConverters[typeof(List<Json19dcItem>)] =
                new SQLiteJsonConverter<List<Json19dcItem>>(Json19dcComplexContext.Default.ListJson19dcItem);
            b.TypeConverters[typeof(List<Json19dcItem[]>)] =
                new SQLiteJsonConverter<List<Json19dcItem[]>>(Json19dcComplexContext.Default.ListJson19dcItemArray);
            b.TypeConverters[typeof(List<Json19dcMeta[]>)] =
                new SQLiteJsonConverter<List<Json19dcMeta[]>>(Json19dcComplexContext.Default.ListJson19dcMetaArray);
            b.TypeConverters[typeof(List<List<int>[]>)] =
                new SQLiteJsonConverter<List<List<int>[]>>(Json19dcComplexContext.Default.NestedIntListArrays);
        });
        db.Table<Json19dcComplexRow>().Schema.CreateTable();
        db.Table<Json19dcComplexRow>().Add(new Json19dcComplexRow
        {
            Id = 1,
            Numbers = SrcNumbers,
            Meta = new Json19dcMeta { Label = "m1", Rank = 4 },
            Items = SrcItems
        });
        return db;
    }

    [Fact]
    public void WholeComplexElementInLiteralWithoutElementConverter()
    {
        using TestDatabase db = CreateDbWithoutItemConverter();

        List<string> expected = SrcItems.Select(i => new[] { i }).ToList()
            .Select(a => a[0].Name + a[0].Qty).ToList();
        List<string> actual = db.Table<Json19dcComplexRow>()
            .Select(r => r.Items.Select(i => new[] { i }).ToList())
            .First()
            .Select(a => a[0].Name + a[0].Qty).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedComplexValueInLiteral()
    {
        using TestDatabase db = CreateDb();
        Json19dcItem extra = new() { Name = "x", Qty = 9 };

        List<string> expected = SrcItems.Select(i => new[] { i, extra }).ToList()
            .Select(a => a[0].Name + "|" + a[1].Name).ToList();
        List<string> actual = db.Table<Json19dcComplexRow>()
            .Select(r => r.Items.Select(i => new[] { i, extra }).ToList())
            .First()
            .Select(a => a[0].Name + "|" + a[1].Name).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OuterComplexColumnInLiteral()
    {
        using TestDatabase db = CreateDb();

        List<string> expected = SrcNumbers.Select(x => new[] { new Json19dcMeta { Label = "m1", Rank = 4 } }).ToList()
            .Select(a => a[0].Label + a[0].Rank).ToList();
        List<string> actual = db.Table<Json19dcComplexRow>()
            .Select(r => r.Numbers.Select(x => new[] { r.Meta }).ToList())
            .First()
            .Select(a => a[0].Label + a[0].Rank).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OuterJsonListColumnInLiteral()
    {
        using TestDatabase db = CreateDb();

        List<List<int>> expected = SrcNumbers.Select(x => new[] { SrcNumbers }).ToList()
            .Select(a => a[0]).ToList();
        List<List<int>> actual = db.Table<Json19dcComplexRow>()
            .Select(r => r.Numbers.Select(x => new[] { r.Numbers }).ToList())
            .First()
            .Select(a => a[0]).ToList();

        Assert.Equal(expected, actual);
    }
}
