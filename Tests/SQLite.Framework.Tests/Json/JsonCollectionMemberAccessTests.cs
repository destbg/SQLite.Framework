using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonNumListRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Nums { get; set; } = [];
}

internal sealed class JsonMapRow
{
    [Key]
    public int Id { get; set; }

    public Dictionary<string, int> Map { get; set; } = [];
}

internal sealed class JsonArrRow
{
    [Key]
    public int Id { get; set; }

    public int[] Arr { get; set; } = [];
}

public class JsonCollectionMemberAccessTests
{
    private static readonly List<int> NumsSeed = [3, 1, 2, 2, 5];

    private static TestDatabase SetupLists()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonNumListRow>().Schema.CreateTable();
        db.Table<JsonNumListRow>().Add(new JsonNumListRow { Id = 1, Nums = NumsSeed });
        db.Table<JsonNumListRow>().Add(new JsonNumListRow { Id = 2, Nums = [] });
        return db;
    }

    private static TestDatabase SetupMaps()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(Dictionary<string, int>)] =
            new SQLiteJsonConverter<Dictionary<string, int>>(TestJsonContext.Default.DictionaryStringInt32));
        db.Table<JsonMapRow>().Schema.CreateTable();
        db.Table<JsonMapRow>().Add(new JsonMapRow { Id = 1, Map = new Dictionary<string, int> { ["k1"] = 2, ["k2"] = 3 } });
        db.Table<JsonMapRow>().Add(new JsonMapRow { Id = 2, Map = [] });
        return db;
    }

    private static TestDatabase SetupArrays()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(int[])] =
            new SQLiteJsonConverter<int[]>(TestJsonContext.Default.Int32Array));
        db.Table<JsonArrRow>().Schema.CreateTable();
        db.Table<JsonArrRow>().Add(new JsonArrRow { Id = 1, Arr = [7, 8, 9] });
        db.Table<JsonArrRow>().Add(new JsonArrRow { Id = 2, Arr = [] });
        return db;
    }

    [Fact]
    public void ListCountPropertyProjects()
    {
        using TestDatabase db = SetupLists();

        List<int> oracle = new List<List<int>> { NumsSeed, new List<int>() }.Select(n => n.Count).ToList();
        List<int> actual = db.Table<JsonNumListRow>().OrderBy(d => d.Id).Select(d => d.Nums.Count).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ListCountPropertyFilters()
    {
        using TestDatabase db = SetupLists();

        int oracle = new List<List<int>> { NumsSeed, new List<int>() }.Count(n => n.Count > 2);
        int actual = db.Table<JsonNumListRow>().Count(d => d.Nums.Count > 2);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DictionaryCountPropertyProjects()
    {
        using TestDatabase db = SetupMaps();

        List<int> oracle = [2, 0];
        List<int> actual = db.Table<JsonMapRow>().OrderBy(d => d.Id).Select(d => d.Map.Count).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DictionaryValuesSumProjects()
    {
        using TestDatabase db = SetupMaps();

        int oracle = new Dictionary<string, int> { ["k1"] = 2, ["k2"] = 3 }.Values.Sum();
        int actual = db.Table<JsonMapRow>().Where(d => d.Id == 1).Select(d => d.Map.Values.Sum()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ArrayLengthProjects()
    {
        using TestDatabase db = SetupArrays();

        List<int> oracle = new List<int[]> { new[] { 7, 8, 9 }, Array.Empty<int>() }.Select(a => a.Length).ToList();
        List<int> actual = db.Table<JsonArrRow>().OrderBy(d => d.Id).Select(d => d.Arr.Length).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ArrayLengthFilters()
    {
        using TestDatabase db = SetupArrays();

        int oracle = new List<int[]> { new[] { 7, 8, 9 }, Array.Empty<int>() }.Count(a => a.Length > 2);
        int actual = db.Table<JsonArrRow>().Count(d => d.Arr.Length > 2);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ArrayLongLengthProjects()
    {
        using TestDatabase db = SetupArrays();

        List<long> oracle = new List<int[]> { new[] { 7, 8, 9 }, Array.Empty<int>() }.Select(a => a.LongLength).ToList();
        List<long> actual = db.Table<JsonArrRow>().OrderBy(d => d.Id).Select(d => d.Arr.LongLength).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DictionaryKeysProjectionIsNotSupported()
    {
        using TestDatabase db = SetupMaps();

        Assert.ThrowsAny<Exception>(() =>
            db.Table<JsonMapRow>().Select(d => d.Map.Keys).First());
    }

    [Fact]
    public void DictionaryValuesProjectionIsNotSupported()
    {
        using TestDatabase db = SetupMaps();

        Assert.ThrowsAny<Exception>(() =>
            db.Table<JsonMapRow>().Select(d => d.Map.Values).First());
    }
}
