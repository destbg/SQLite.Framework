using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public sealed class JsonMemberItem
{
    public int Price { get; set; }

    public string Name { get; set; } = "";
}

internal sealed class JsonMemberRow
{
    [Key]
    public int Id { get; set; }

    public List<JsonMemberItem> Items { get; set; } = [];
}

[JsonSerializable(typeof(List<JsonMemberItem>))]
internal partial class JsonMemberContext : JsonSerializerContext;

public class JsonListSelectMemberProjectionTests
{
    [Fact]
    public void SelectScalarMemberFromObjectListToListMaterializes()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<JsonMemberItem>)] =
                new SQLiteJsonConverter<List<JsonMemberItem>>(JsonMemberContext.Default.ListJsonMemberItem));
        db.Table<JsonMemberRow>().Schema.CreateTable();

        List<JsonMemberItem> seed = [new() { Price = 10, Name = "a" }, new() { Price = 20, Name = "b" }];
        db.Table<JsonMemberRow>().Add(new JsonMemberRow { Id = 1, Items = seed });

        List<int> expected = seed.Select(x => x.Price).ToList();
        List<int> actual = db.Table<JsonMemberRow>().Select(r => r.Items.Select(x => x.Price).ToList()).First();
        List<string> expectedNames = seed.Select(x => x.Name).ToList();
        List<string> actualNames = db.Table<JsonMemberRow>().Select(r => r.Items.Select(x => x.Name).ToList()).First();

        Assert.Equal(expected, actual);
        Assert.Equal(expectedNames, actualNames);
    }
}
