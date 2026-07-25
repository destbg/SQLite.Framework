using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonNamingPolicyPayload
{
    public string Name { get; set; } = "";

    public int Score { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(JsonNamingPolicyPayload))]
public partial class JsonNamingPolicyContext : JsonSerializerContext;

[Table("JsonNamingPolicyDoc")]
public class JsonNamingPolicyDoc
{
    [Key]
    public int Id { get; set; }

    public JsonNamingPolicyPayload Data { get; set; } = new();
}

public class JsonNamingPolicyMemberQueryTests
{
    private static TestDatabase CreateDatabase()
    {
        return new TestDatabase(b =>
        {
            b.AddTypeConverter<JsonNamingPolicyPayload>(
                new SQLiteJsonConverter<JsonNamingPolicyPayload>(JsonNamingPolicyContext.Default.JsonNamingPolicyPayload));
        });
    }

    [Fact]
    public void WhereOnMemberMatchesRow()
    {
        using TestDatabase db = CreateDatabase();
        db.Table<JsonNamingPolicyDoc>().Schema.CreateTable();
        db.Table<JsonNamingPolicyDoc>().Add(new JsonNamingPolicyDoc
        {
            Id = 1,
            Data = new JsonNamingPolicyPayload { Name = "abc", Score = 7 },
        });

        List<int> ids = db.Table<JsonNamingPolicyDoc>()
            .Where(r => r.Data.Name == "abc")
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void SelectMemberReadsStoredValue()
    {
        using TestDatabase db = CreateDatabase();
        db.Table<JsonNamingPolicyDoc>().Schema.CreateTable();
        db.Table<JsonNamingPolicyDoc>().Add(new JsonNamingPolicyDoc
        {
            Id = 1,
            Data = new JsonNamingPolicyPayload { Name = "abc", Score = 7 },
        });

        List<int> scores = db.Table<JsonNamingPolicyDoc>()
            .Select(r => r.Data.Score)
            .ToList();

        Assert.Equal([7], scores);
    }
}
