using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(Dictionary<DateTime, int>))]
internal partial class TemporalDictKeyContext : JsonSerializerContext;

internal sealed class TemporalDictKeyRow
{
    [Key]
    public int Id { get; set; }

    public Dictionary<DateTime, int> Map { get; set; } = [];
}

public class JsonDictionaryTemporalKeyTests
{
    private static readonly DateTime Key = new(2024, 5, 6, 7, 8, 9);

    private static TestDatabase Seed(out Dictionary<DateTime, int> local)
    {
        local = new Dictionary<DateTime, int> { [Key] = 10 };
        TestDatabase db = new(b => b.AddJsonContext(TemporalDictKeyContext.Default));
        db.Table<TemporalDictKeyRow>().Schema.CreateTable();
        db.Table<TemporalDictKeyRow>().Add(new TemporalDictKeyRow { Id = 1, Map = new Dictionary<DateTime, int> { [Key] = 10 } });
        return db;
    }

    [Fact]
    public void ContainsKeyWithDateTimeConstant()
    {
        using TestDatabase db = Seed(out Dictionary<DateTime, int> local);

        bool expected = local.ContainsKey(Key);
        bool actual = db.Table<TemporalDictKeyRow>().Select(r => r.Map.ContainsKey(Key)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IndexerWithDateTimeConstant()
    {
        using TestDatabase db = Seed(out Dictionary<DateTime, int> local);

        int expected = local[Key];
        int actual = db.Table<TemporalDictKeyRow>().Select(r => r.Map[Key]).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void KeysContainsDateTimeConstant()
    {
        using TestDatabase db = Seed(out Dictionary<DateTime, int> local);

        bool expected = local.Keys.Contains(Key);
        bool actual = db.Table<TemporalDictKeyRow>().Select(r => r.Map.Keys.Contains(Key)).First();

        Assert.Equal(expected, actual);
    }
}
