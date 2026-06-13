using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonRangeListRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Nums { get; set; } = [];
}

internal sealed class JsonKeyMapRow
{
    [Key]
    public int Id { get; set; }

    public Dictionary<string, int> Map { get; set; } = [];
}

public class JsonCollectionArgumentShapeTests
{
    private static readonly List<int> NumsSeed = [3, 1, 2, 2, 5];

    private static TestDatabase SetupLists()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonRangeListRow>().Schema.CreateTable();
        db.Table<JsonRangeListRow>().Add(new JsonRangeListRow { Id = 1, Nums = NumsSeed });
        return db;
    }

    [Fact]
    public void GetRangeBeyondEndReturnsTheItemsThatFit()
    {
        using TestDatabase db = SetupLists();

        Assert.Throws<ArgumentException>(() => NumsSeed.GetRange(3, 10));

        List<int> actual = db.Table<JsonRangeListRow>().Where(d => d.Id == 1).Select(d => d.Nums.GetRange(3, 10)).First();

        Assert.Equal(NumsSeed.Skip(3).ToList(), actual);
    }

    [Fact]
    public void SkipWithRowArgumentIsNotSupported()
    {
        using TestDatabase db = SetupLists();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<JsonRangeListRow>().Select(d => d.Nums.Skip(d.Id).Count()).First());
    }

    [Fact]
    public void TakeWithRowArgumentIsNotSupported()
    {
        using TestDatabase db = SetupLists();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<JsonRangeListRow>().Select(d => d.Nums.Take(d.Id).Count()).First());
    }

    [Fact]
    public void DictionaryKeysContainsProjects()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(Dictionary<string, int>)] =
            new SQLiteJsonConverter<Dictionary<string, int>>(TestJsonContext.Default.DictionaryStringInt32));
        using (db)
        {
            db.Table<JsonKeyMapRow>().Schema.CreateTable();
            db.Table<JsonKeyMapRow>().Add(new JsonKeyMapRow { Id = 1, Map = new Dictionary<string, int> { ["k1"] = 2 } });

            bool oracle = new Dictionary<string, int> { ["k1"] = 2 }.Keys.Contains("k1");
            bool actual = db.Table<JsonKeyMapRow>().Select(d => d.Map.Keys.Contains("k1")).First();

            Assert.Equal(oracle, actual);
        }
    }
}
