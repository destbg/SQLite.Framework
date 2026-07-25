using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal enum DictColor
{
    Red = 1,
    Blue = 2,
}

[JsonSerializable(typeof(Dictionary<DictColor, int>))]
internal partial class DictColorMapContext : JsonSerializerContext;

internal sealed class EnumKeyMapRow
{
    [Key]
    public int Id { get; set; }

    public Dictionary<DictColor, int> Map { get; set; } = [];
}

public class JsonDictionaryEnumKeysContainsDefaultStorageTests
{
    [Fact]
    public void KeysContainsEnumConstant()
    {
        using TestDatabase db = new(b => b.AddJsonContext(DictColorMapContext.Default));
        db.Table<EnumKeyMapRow>().Schema.CreateTable();
        db.Table<EnumKeyMapRow>().Add(new EnumKeyMapRow { Id = 1, Map = new Dictionary<DictColor, int> { [DictColor.Red] = 1 } });

        Dictionary<DictColor, int> map = new() { [DictColor.Red] = 1 };
        bool expected = map.Keys.Contains(DictColor.Red);
        Assert.True(expected);

        bool actual = db.Table<EnumKeyMapRow>().Select(r => r.Map.Keys.Contains(DictColor.Red)).First();
        Assert.Equal(expected, actual);
    }
}
