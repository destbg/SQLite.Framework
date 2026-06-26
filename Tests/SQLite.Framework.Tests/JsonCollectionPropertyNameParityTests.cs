using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonNamedCity
{
    [JsonPropertyName("city_name")]
    public string City { get; set; } = "";

    public int Score { get; set; }
}

internal sealed class JsonCityHolder
{
    public JsonNamedCity Inner { get; set; } = new();
}

[JsonSerializable(typeof(List<JsonNamedCity>))]
[JsonSerializable(typeof(List<JsonCityHolder>))]
internal partial class JsonNamedCityContext : JsonSerializerContext;

internal sealed class JsonNamedCityRow
{
    [Key]
    public int Id { get; set; }

    public List<JsonNamedCity> Items { get; set; } = [];
}

internal sealed class JsonCityHolderRow
{
    [Key]
    public int Id { get; set; }

    public List<JsonCityHolder> Items { get; set; } = [];
}

public class JsonCollectionPropertyNameParityTests
{
    private static TestDatabase CreateDb()
    {
        return new TestDatabase(b =>
        {
            b.TypeConverters[typeof(List<JsonNamedCity>)] =
                new SQLiteJsonConverter<List<JsonNamedCity>>(JsonNamedCityContext.Default.ListJsonNamedCity);
            b.TypeConverters[typeof(List<JsonCityHolder>)] =
                new SQLiteJsonConverter<List<JsonCityHolder>>(JsonNamedCityContext.Default.ListJsonCityHolder);
        });
    }

    [Fact]
    public void Any_OnRenamedElementProperty_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();
        db.Table<JsonNamedCityRow>().Schema.CreateTable();
        JsonNamedCityRow[] rows =
        [
            new() { Id = 1, Items = [new JsonNamedCity { City = "Springfield", Score = 1 }] },
            new() { Id = 2, Items = [new JsonNamedCity { City = "Portland", Score = 2 }] },
        ];
        db.Table<JsonNamedCityRow>().AddRange(rows);

        List<int> oracle = rows.Where(c => c.Items.Any(a => a.City == "Springfield")).Select(c => c.Id).ToList();
        List<int> actual = db.Table<JsonNamedCityRow>().Where(c => c.Items.Any(a => a.City == "Springfield")).Select(c => c.Id).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Count_OnRenamedElementProperty_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();
        db.Table<JsonNamedCityRow>().Schema.CreateTable();
        db.Table<JsonNamedCityRow>().Add(new JsonNamedCityRow
        {
            Id = 1,
            Items = [new JsonNamedCity { City = "A", Score = 5 }, new JsonNamedCity { City = "B", Score = 9 }]
        });

        int oracle = new[] { new JsonNamedCity { City = "A", Score = 5 }, new JsonNamedCity { City = "B", Score = 9 } }.Count(a => a.City == "A");
        int actual = db.Table<JsonNamedCityRow>().Select(r => r.Items.Count(a => a.City == "A")).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NestedAny_OnRenamedElementProperty_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();
        db.Table<JsonCityHolderRow>().Schema.CreateTable();
        JsonCityHolderRow[] rows =
        [
            new() { Id = 1, Items = [new JsonCityHolder { Inner = new JsonNamedCity { City = "Springfield" } }] },
            new() { Id = 2, Items = [new JsonCityHolder { Inner = new JsonNamedCity { City = "Portland" } }] },
        ];
        db.Table<JsonCityHolderRow>().AddRange(rows);

        List<int> oracle = rows.Where(c => c.Items.Any(a => a.Inner.City == "Springfield")).Select(c => c.Id).ToList();
        List<int> actual = db.Table<JsonCityHolderRow>().Where(c => c.Items.Any(a => a.Inner.City == "Springfield")).Select(c => c.Id).ToList();

        Assert.Equal(oracle, actual);
    }
}
