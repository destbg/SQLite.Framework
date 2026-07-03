using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal enum ShadeKind
{
    Red = 1,
    Blue = 2,
}

[JsonSerializable(typeof(Dictionary<ShadeKind, int>))]
internal partial class ShadeKindDictContext : JsonSerializerContext;

internal sealed class ShadeKindDictRow
{
    [Key]
    public int Id { get; set; }

    public ShadeKind Shade { get; set; }

    public Dictionary<ShadeKind, int> Map { get; set; } = [];
}

public class JsonDictionaryEnumKeysContainsColumnValueTests
{
    [Fact]
    public void KeysContainsEnumColumnDefaultStorage()
    {
        using TestDatabase db = new(b => b.AddJsonContext(ShadeKindDictContext.Default));
        db.Table<ShadeKindDictRow>().Schema.CreateTable();
        db.Table<ShadeKindDictRow>().Add(new ShadeKindDictRow
        {
            Id = 1,
            Shade = ShadeKind.Red,
            Map = new Dictionary<ShadeKind, int> { [ShadeKind.Red] = 10 }
        });

        ShadeKindDictRow local = new()
        {
            Shade = ShadeKind.Red,
            Map = new Dictionary<ShadeKind, int> { [ShadeKind.Red] = 10 }
        };
        bool expected = local.Map.Keys.Contains(local.Shade);
        Assert.True(expected);

        bool actual = db.Table<ShadeKindDictRow>().Select(r => r.Map.Keys.Contains(r.Shade)).First();
        Assert.Equal(expected, actual);
    }
}
