using SQLite.Framework.Enums;
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
[JsonSerializable(typeof(Dictionary<bool, int>))]
internal partial class ShadeKindDictContext : JsonSerializerContext;

internal sealed class BoolKeyDictRow
{
    [Key]
    public int Id { get; set; }

    public Dictionary<bool, int> Flags { get; set; } = [];
}

internal sealed class ShadeKindDictRow
{
    [Key]
    public int Id { get; set; }

    public ShadeKind Shade { get; set; }

    public ShadeKind? Shade2 { get; set; }

    public Dictionary<ShadeKind, int> Map { get; set; } = [];
}

public class JsonDictionaryEnumKeysContainsColumnValueTests
{
    [Fact]
    public void ContainsKeyWithConstantEnumKeyFilters()
    {
        using TestDatabase db = new(b => b.AddJsonContext(ShadeKindDictContext.Default));
        db.Table<ShadeKindDictRow>().Schema.CreateTable();
        db.Table<ShadeKindDictRow>().Add(new ShadeKindDictRow
        {
            Id = 1,
            Shade = ShadeKind.Red,
            Map = new Dictionary<ShadeKind, int> { [ShadeKind.Red] = 10 }
        });

        List<ShadeKindDictRow> memory =
        [
            new ShadeKindDictRow { Id = 1, Shade = ShadeKind.Red, Map = new Dictionary<ShadeKind, int> { [ShadeKind.Red] = 10 } }
        ];
        List<int> expected = memory.Where(r => r.Map.ContainsKey(ShadeKind.Red)).Select(r => r.Id).ToList();
        Assert.Equal([1], expected);

        List<int> actual = db.Table<ShadeKindDictRow>().Where(r => r.Map.ContainsKey(ShadeKind.Red)).Select(r => r.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IndexerWithConstantEnumKeyProjects()
    {
        using TestDatabase db = new(b => b.AddJsonContext(ShadeKindDictContext.Default));
        db.Table<ShadeKindDictRow>().Schema.CreateTable();
        db.Table<ShadeKindDictRow>().Add(new ShadeKindDictRow
        {
            Id = 1,
            Shade = ShadeKind.Red,
            Map = new Dictionary<ShadeKind, int> { [ShadeKind.Red] = 10 }
        });

        List<ShadeKindDictRow> memory =
        [
            new ShadeKindDictRow { Id = 1, Shade = ShadeKind.Red, Map = new Dictionary<ShadeKind, int> { [ShadeKind.Red] = 10 } }
        ];
        List<int> expected = memory.Select(r => r.Map[ShadeKind.Red]).ToList();
        Assert.Equal([10], expected);

        List<int> actual = db.Table<ShadeKindDictRow>().Select(r => r.Map[ShadeKind.Red]).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsKeyWithConstantBoolKeyThrows()
    {
        using TestDatabase db = new(b => b.AddJsonContext(ShadeKindDictContext.Default));
        db.Table<BoolKeyDictRow>().Schema.CreateTable();
        db.Table<BoolKeyDictRow>().Add(new BoolKeyDictRow { Id = 1, Flags = new Dictionary<bool, int> { [true] = 1 } });

        Exception? ex = Record.Exception(() => db.Table<BoolKeyDictRow>()
            .Where(r => r.Flags.ContainsKey(true))
            .Select(r => r.Id)
            .ToList());

        Assert.IsType<NotSupportedException>(ex);
    }

    [Fact]
    public void KeysContainsNullableEnumColumnValueDefaultStorage()
    {
        using TestDatabase db = new(b => b.AddJsonContext(ShadeKindDictContext.Default));
        db.Table<ShadeKindDictRow>().Schema.CreateTable();
        db.Table<ShadeKindDictRow>().Add(new ShadeKindDictRow
        {
            Id = 1,
            Shade = ShadeKind.Red,
            Shade2 = ShadeKind.Red,
            Map = new Dictionary<ShadeKind, int> { [ShadeKind.Red] = 10 }
        });

        ShadeKindDictRow local = new()
        {
            Shade2 = ShadeKind.Red,
            Map = new Dictionary<ShadeKind, int> { [ShadeKind.Red] = 10 }
        };
        bool expected = local.Map.Keys.Contains(local.Shade2!.Value);
        Assert.True(expected);

        bool actual = db.Table<ShadeKindDictRow>().Select(r => r.Map.Keys.Contains(r.Shade2!.Value)).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void KeysContainsEnumColumnTextStorage()
    {
        using TestDatabase db = new(b =>
        {
            b.AddJsonContext(ShadeKindDictContext.Default);
            b.EnumStorage = EnumStorageMode.Text;
        });
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
