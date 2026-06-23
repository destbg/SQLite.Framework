using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal enum JsonStorageTag
{
    A = 0,
    B = 1,
    C = 2,
}

[JsonSerializable(typeof(List<JsonStorageTag>))]
internal partial class JsonStorageTagContext : JsonSerializerContext;

internal sealed class JsonStorageTagRow
{
    [Key]
    public int Id { get; set; }

    public List<JsonStorageTag> Tags { get; set; } = new();
}

public class JsonListEnumContainsStorageParityTests
{
    [Fact]
    public void ContainsOnJsonListEnum_UnderTextEnumStorage_DoesNotMatchNumericJson()
    {
        using TestDatabase db = new(b =>
        {
            b.UseEnumStorage(EnumStorageMode.Text);
            b.AddJsonContext(JsonStorageTagContext.Default);
        });
        db.Table<JsonStorageTagRow>().Schema.CreateTable();

        db.Table<JsonStorageTagRow>().Add(new JsonStorageTagRow { Id = 1, Tags = [JsonStorageTag.A, JsonStorageTag.B] });
        db.Table<JsonStorageTagRow>().Add(new JsonStorageTagRow { Id = 2, Tags = [JsonStorageTag.C] });

        List<int> actual = db.Table<JsonStorageTagRow>().Where(r => r.Tags.Contains(JsonStorageTag.B)).Select(r => r.Id).ToList();

        Assert.Empty(actual);
    }
}
