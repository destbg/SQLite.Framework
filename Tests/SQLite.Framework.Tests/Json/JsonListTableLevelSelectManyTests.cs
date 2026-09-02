using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(List<TableSelectManyItem>))]
[JsonSerializable(typeof(Dictionary<string, int>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(byte[]))]
internal partial class TableSelectManyListContext : JsonSerializerContext;

internal sealed class TableSelectManyItem
{
    public string Label { get; set; } = "";

    [JsonPropertyName("score")]
    public int Score { get; set; }
}

internal sealed class TableSelectManyObjectRow
{
    [Key]
    public int Id { get; set; }

    public List<TableSelectManyItem> Items { get; set; } = [];
}

internal sealed class TableSelectManyRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Values { get; set; } = [];
}

internal sealed class TableSelectManyScalarRow
{
    [Key]
    public int Id { get; set; }

    public byte[] Data { get; set; } = [];

    public string Text { get; set; } = "";
}

internal sealed class TableSelectManyMapRow
{
    [Key]
    public int Id { get; set; }

    public Dictionary<string, int> Map { get; set; } = [];
}

public class JsonListTableLevelSelectManyTests
{
    [Fact]
    public void SelectManyOverJsonByteArrayThrows()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(byte[])] =
            new SQLiteJsonConverter<byte[]>(TableSelectManyListContext.Default.ByteArray));
        db.Table<TableSelectManyScalarRow>().Schema.CreateTable();
        db.Table<TableSelectManyScalarRow>().Add(new TableSelectManyScalarRow { Id = 1, Data = [1, 2] });

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<TableSelectManyScalarRow>()
            .SelectMany(r => r.Data)
            .ToList());

        Assert.Contains("JSON string or byte[] source", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectManyOverJsonDictionaryThrows()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(Dictionary<string, int>)] =
            new SQLiteJsonConverter<Dictionary<string, int>>(TableSelectManyListContext.Default.DictionaryStringInt32));
        db.Table<TableSelectManyMapRow>().Schema.CreateTable();
        db.Table<TableSelectManyMapRow>().Add(new TableSelectManyMapRow { Id = 1, Map = new() { ["a"] = 1 } });

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<TableSelectManyMapRow>()
            .SelectMany(r => r.Map)
            .ToList());

        Assert.Contains("Dictionary<TKey, TValue> source", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectManyOverJsonStringThrows()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(string)] =
            new SQLiteJsonConverter<string>(TableSelectManyListContext.Default.String));
        db.Table<TableSelectManyScalarRow>().Schema.CreateTable();
        db.Table<TableSelectManyScalarRow>().Add(new TableSelectManyScalarRow { Id = 1, Text = "ab" });

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<TableSelectManyScalarRow>()
            .SelectMany(r => r.Text)
            .ToList());

        Assert.Contains("JSON string or byte[] source", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectManyOverJsonObjectListProjectsProperties()
    {
        using TestDatabase db = new(b => b.AddJsonContext(TableSelectManyListContext.Default));
        db.Table<TableSelectManyObjectRow>().Schema.CreateTable();
        List<TableSelectManyObjectRow> memory =
        [
            new TableSelectManyObjectRow
            {
                Id = 1,
                Items =
                [
                    new TableSelectManyItem { Label = "a", Score = 3 },
                    new TableSelectManyItem { Label = "b", Score = 7 },
                ]
            },
            new TableSelectManyObjectRow { Id = 2, Items = [new TableSelectManyItem { Label = "c", Score = 5 }] },
            new TableSelectManyObjectRow { Id = 3, Items = [] },
        ];
        db.Table<TableSelectManyObjectRow>().AddRange(memory);

        List<(string Label, int Score)> expected = memory
            .SelectMany(r => r.Items)
            .Where(i => i.Score >= 5)
            .OrderBy(i => i.Score)
            .Select(i => (i.Label, i.Score))
            .ToList();
        List<(string Label, int Score)> actual = db.Table<TableSelectManyObjectRow>()
            .SelectMany(r => r.Items)
            .Where(i => i.Score >= 5)
            .OrderBy(i => i.Score)
            .Select(i => new ValueTuple<string, int>(i.Label, i.Score))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectManyOverJsonListColumnMatchesLinq()
    {
        using TestDatabase db = new(b => b.AddJsonContext(TableSelectManyListContext.Default));
        db.Table<TableSelectManyRow>().Schema.CreateTable();
        List<TableSelectManyRow> memory =
        [
            new TableSelectManyRow { Id = 1, Values = [1, 2] },
            new TableSelectManyRow { Id = 2, Values = [2, 3] },
            new TableSelectManyRow { Id = 3, Values = [] },
        ];
        db.Table<TableSelectManyRow>().AddRange(memory);

        List<int> expected = memory.SelectMany(r => r.Values).ToList();
        List<int> actual = db.Table<TableSelectManyRow>().OrderBy(r => r.Id).SelectMany(r => r.Values).ToList();
        List<int> expectedSorted = memory.SelectMany(r => r.Values).OrderBy(v => v).ToList();
        List<int> actualSorted = db.Table<TableSelectManyRow>().SelectMany(r => r.Values).OrderBy(v => v).ToList();
        List<int> expectedFiltered = memory.SelectMany(r => r.Values.Where(v => v % 2 == 0)).ToList();
        List<int> actualFiltered = db.Table<TableSelectManyRow>().OrderBy(r => r.Id)
            .SelectMany(r => r.Values.Where(v => v % 2 == 0)).ToList();
        List<(int Id, int Value)> expectedPairs = memory.SelectMany(r => r.Values, (r, v) => (r.Id, v)).ToList();
        List<(int Id, int Value)> actualPairs = db.Table<TableSelectManyRow>().OrderBy(r => r.Id)
            .SelectMany(r => r.Values, (r, v) => new ValueTuple<int, int>(r.Id, v)).ToList();

        Assert.Equal(expected, actual);
        Assert.Equal(expectedSorted, actualSorted);
        Assert.Equal(expectedFiltered, actualFiltered);
        Assert.Equal(expectedPairs, actualPairs);
    }
}
