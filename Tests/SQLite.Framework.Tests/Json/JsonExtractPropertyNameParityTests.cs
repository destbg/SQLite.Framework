using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonRenamedProfile
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = "";
}

[JsonSerializable(typeof(JsonRenamedProfile))]
internal partial class JsonRenamedProfileContext : JsonSerializerContext;

internal sealed class JsonRenamedProfileRow
{
    [Key]
    public int Id { get; set; }

    public JsonRenamedProfile Profile { get; set; } = new();
}

public class JsonExtractPropertyNameParityTests
{
    [Fact]
    public void WhereOnJsonPropertyWithJsonPropertyName_MatchesLinqToObjects()
    {
        using TestDatabase db = new(b => b.AddJsonContext(JsonRenamedProfileContext.Default));
        db.Table<JsonRenamedProfileRow>().Schema.CreateTable();

        List<JsonRenamedProfileRow> rows =
        [
            new JsonRenamedProfileRow { Id = 1, Profile = new JsonRenamedProfile { DisplayName = "Alice" } },
            new JsonRenamedProfileRow { Id = 2, Profile = new JsonRenamedProfile { DisplayName = "Bob" } },
        ];
        foreach (JsonRenamedProfileRow row in rows)
        {
            db.Table<JsonRenamedProfileRow>().Add(row);
        }

        List<int> oracle = rows.Where(p => p.Profile.DisplayName == "Alice").Select(p => p.Id).ToList();
        List<int> actual = db.Table<JsonRenamedProfileRow>().Where(p => p.Profile.DisplayName == "Alice").Select(p => p.Id).ToList();

        Assert.Equal(oracle, actual);
    }
}
