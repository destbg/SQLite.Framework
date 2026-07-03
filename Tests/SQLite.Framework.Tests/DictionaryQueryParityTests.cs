using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(Dictionary<string, int>))]
internal partial class JdEdgeJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(Dictionary<int, int>))]
internal partial class JdEdgeIntJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(JdNestedHolder))]
internal partial class JdNestedJsonContext : JsonSerializerContext;

internal sealed class JdNestedHolder
{
    public Dictionary<string, int> Inner { get; set; } = new();
}

internal sealed class JdNestedDictRow
{
    [Key]
    [Column]
    public int Id { get; set; }

    [Column]
    public JdNestedHolder Holder { get; set; } = new();
}

internal sealed class JdEdgeMapRow
{
    [Key]
    [Column]
    public int Id { get; set; }

    [Column]
    public Dictionary<string, int> Map { get; set; } = new();
}

internal sealed class JdEdgeIntMapRow
{
    [Key]
    [Column]
    public int Id { get; set; }

    [Column]
    public Dictionary<int, int> Map { get; set; } = new();
}

public class DictionaryQueryParityTests
{
    private static TestDatabase MapDb()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(Dictionary<string, int>)] =
            new SQLiteJsonConverter<Dictionary<string, int>>(JdEdgeJsonContext.Default.DictionaryStringInt32));
        db.Table<JdEdgeMapRow>().Schema.CreateTable();
        return db;
    }

    private static List<Dictionary<string, int>> MapSeed()
    {
        return new()
        {
            new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 },
            new Dictionary<string, int> { ["a"] = 5 },
            new Dictionary<string, int>(),
        };
    }

    private static void SeedMaps(TestDatabase db)
    {
        List<Dictionary<string, int>> seed = MapSeed();
        for (int i = 0; i < seed.Count; i++)
        {
            db.Table<JdEdgeMapRow>().Add(new JdEdgeMapRow { Id = i + 1, Map = seed[i] });
        }
    }

    [Fact]
    public void IndexerExistingKeyProjects()
    {
        using TestDatabase db = MapDb();
        SeedMaps(db);
        var expected = MapSeed().Where(m => m.ContainsKey("a")).Select(m => m["a"]).ToList();
        var actual = db.Table<JdEdgeMapRow>().Where(m => m.Map.ContainsKey("a")).OrderBy(m => m.Id).Select(m => m.Map["a"]).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsKeyExistingFilters()
    {
        using TestDatabase db = MapDb();
        SeedMaps(db);
        var expected = MapSeed().Select((m, i) => (Id: i + 1, Has: m.ContainsKey("b"))).Where(x => x.Has).Select(x => x.Id).ToList();
        var actual = db.Table<JdEdgeMapRow>().Where(m => m.Map.ContainsKey("b")).OrderBy(m => m.Id).Select(m => m.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsValuePairFilters()
    {
        using TestDatabase db = MapDb();
        SeedMaps(db);
        var pair = new KeyValuePair<string, int>("a", 1);
        Assert.Throws<System.NotSupportedException>(() =>
            db.Table<JdEdgeMapRow>().Where(m => m.Map.Contains(pair)).OrderBy(m => m.Id).Select(m => m.Id).ToList());
    }

    [Fact]
    public void IndexerInWhereFilters()
    {
        using TestDatabase db = MapDb();
        SeedMaps(db);
        var expected = MapSeed().Select((m, i) => (Id: i + 1, m)).Where(x => x.m.ContainsKey("a") && x.m["a"] > 3).Select(x => x.Id).ToList();
        var actual = db.Table<JdEdgeMapRow>().Where(m => m.Map.ContainsKey("a") && m.Map["a"] > 3).OrderBy(m => m.Id).Select(m => m.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IndexerDirectlyInWhereFilters()
    {
        using TestDatabase db = MapDb();
        SeedMaps(db);
        var expected = MapSeed().Select((m, i) => (Id: i + 1, m)).Where(x => x.m.ContainsKey("a") && x.m["a"] == 5).Select(x => x.Id).ToList();
        var actual = db.Table<JdEdgeMapRow>().Where(m => m.Map["a"] == 5).OrderBy(m => m.Id).Select(m => m.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsKeyInOrderByOrders()
    {
        using TestDatabase db = MapDb();
        SeedMaps(db);
        var expected = MapSeed().Select((m, i) => (Id: i + 1, m)).OrderByDescending(x => x.m.ContainsKey("b")).ThenBy(x => x.Id).Select(x => x.Id).ToList();
        var actual = db.Table<JdEdgeMapRow>().OrderByDescending(m => m.Map.ContainsKey("b")).ThenBy(m => m.Id).Select(m => m.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsValueNotSupported()
    {
        using TestDatabase db = MapDb();
        SeedMaps(db);
        Assert.Throws<NotSupportedException>(() =>
            db.Table<JdEdgeMapRow>().Where(m => m.Map.ContainsValue(5)).Select(m => m.Id).ToList());
    }

    [Fact]
    public void ContainsKeyWithColumnDerivedKeyNotSupported()
    {
        using TestDatabase db = MapDb();
        SeedMaps(db);
        Assert.Throws<NotSupportedException>(() =>
            db.Table<JdEdgeMapRow>().Where(m => m.Map.ContainsKey(m.Id.ToString())).Select(m => m.Id).ToList());
    }

    [Fact]
    public void IntKeyDictionaryContainsKeyFilters()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(Dictionary<int, int>)] =
            new SQLiteJsonConverter<Dictionary<int, int>>(JdEdgeIntJsonContext.Default.DictionaryInt32Int32));
        db.Table<JdEdgeIntMapRow>().Schema.CreateTable();
        db.Table<JdEdgeIntMapRow>().Add(new JdEdgeIntMapRow { Id = 1, Map = new Dictionary<int, int> { [1] = 10 } });

        List<JdEdgeIntMapRow> memory = [new JdEdgeIntMapRow { Id = 1, Map = new Dictionary<int, int> { [1] = 10 } }];
        List<int> expected = memory.Where(m => m.Map.ContainsKey(1)).Select(m => m.Id).ToList();
        Assert.Equal([1], expected);

        List<int> actual = db.Table<JdEdgeIntMapRow>().Where(m => m.Map.ContainsKey(1)).Select(m => m.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedDictionaryContainsKeyFilters()
    {
        using TestDatabase db = new(b => b.AddJsonContext(JdNestedJsonContext.Default));
        db.Table<JdNestedDictRow>().Schema.CreateTable();
        List<JdNestedDictRow> seed =
        [
            new JdNestedDictRow { Id = 1, Holder = new JdNestedHolder { Inner = new Dictionary<string, int> { ["a"] = 1 } } },
            new JdNestedDictRow { Id = 2, Holder = new JdNestedHolder { Inner = new Dictionary<string, int> { ["b"] = 2 } } },
        ];
        foreach (JdNestedDictRow r in seed)
        {
            db.Table<JdNestedDictRow>().Add(r);
        }

        List<int> expected = seed.Where(r => r.Holder.Inner.ContainsKey("a")).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<JdNestedDictRow>().Where(r => r.Holder.Inner.ContainsKey("a")).OrderBy(r => r.Id).Select(r => r.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConditionalReceiverContainsKeyFilters()
    {
        using TestDatabase db = MapDb();
        SeedMaps(db);
        var expected = MapSeed().Select((m, i) => (Id: i + 1, m)).Where(x => (x.Id > 5 ? x.m : x.m).ContainsKey("a")).Select(x => x.Id).ToList();
        var actual = db.Table<JdEdgeMapRow>().Where(m => (m.Id > 5 ? m.Map : m.Map).ContainsKey("a")).OrderBy(m => m.Id).Select(m => m.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConditionalReceiverIndexerFilters()
    {
        using TestDatabase db = MapDb();
        SeedMaps(db);
        var expected = MapSeed().Select((m, i) => (Id: i + 1, m)).Where(x => x.m.ContainsKey("a") && x.m["a"] == 5).Select(x => x.Id).ToList();
        var actual = db.Table<JdEdgeMapRow>().Where(m => (m.Id > 5 ? m.Map : m.Map)["a"] == 5).OrderBy(m => m.Id).Select(m => m.Id).ToList();
        Assert.Equal(expected, actual);
    }
}
