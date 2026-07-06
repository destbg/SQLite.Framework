using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum JsonStringEnumState
{
    Draft = 0,
    Active = 1,
}

public class JsonStringEnumPayload
{
    public string Name { get; set; } = "";

    public JsonStringEnumState State { get; set; }
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(JsonStringEnumPayload))]
public partial class JsonStringEnumContext : JsonSerializerContext;

[Table("JsonStringEnumDoc")]
public class JsonStringEnumDoc
{
    [Key]
    public int Id { get; set; }

    public JsonStringEnumPayload Data { get; set; } = new();
}

public class JsonStringEnumMemberComparisonTests
{
    [Fact]
    public void WhereOnEnumMemberMatchesRow()
    {
        using TestDatabase db = new(b =>
        {
            b.AddTypeConverter<JsonStringEnumPayload>(
                new SQLiteJsonConverter<JsonStringEnumPayload>(JsonStringEnumContext.Default.JsonStringEnumPayload));
        });
        db.Table<JsonStringEnumDoc>().Schema.CreateTable();
        db.Table<JsonStringEnumDoc>().Add(new JsonStringEnumDoc
        {
            Id = 1,
            Data = new JsonStringEnumPayload { Name = "a", State = JsonStringEnumState.Active },
        });

        List<int> ids = db.Table<JsonStringEnumDoc>()
            .Where(r => r.Data.State == JsonStringEnumState.Active)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], ids);
    }
}
