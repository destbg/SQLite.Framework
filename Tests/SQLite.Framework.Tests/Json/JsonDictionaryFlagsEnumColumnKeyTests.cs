using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Flags]
public enum FlagsKeyAccess
{
    None = 0,
    Read = 1,
    Write = 2,
}

[JsonSerializable(typeof(Dictionary<FlagsKeyAccess, int>))]
internal partial class FlagsKeyDictContext : JsonSerializerContext;

internal sealed class FlagsKeyDictRow
{
    [Key]
    public int Id { get; set; }

    public FlagsKeyAccess Shade { get; set; }

    public Dictionary<FlagsKeyAccess, int> Map { get; set; } = [];
}

public class JsonDictionaryFlagsEnumColumnKeyTests
{
    [Fact]
    public void KeysContainsCombinedFlagsColumnValueDoesNotMatch()
    {
        using TestDatabase db = new(b => b.AddJsonContext(FlagsKeyDictContext.Default));
        db.Table<FlagsKeyDictRow>().Schema.CreateTable();
        FlagsKeyDictRow row = new()
        {
            Id = 1,
            Shade = FlagsKeyAccess.Read | FlagsKeyAccess.Write,
            Map = new Dictionary<FlagsKeyAccess, int> { [FlagsKeyAccess.Read | FlagsKeyAccess.Write] = 1 },
        };
        db.Table<FlagsKeyDictRow>().Add(row);

        bool memory = row.Map.Keys.Contains(row.Shade);
        Assert.True(memory);

        bool actual = db.Table<FlagsKeyDictRow>().Select(r => r.Map.Keys.Contains(r.Shade)).First();

        Assert.False(actual);
    }

    [Fact]
    public void KeysContainsSingleFlagColumnValueMatches()
    {
        using TestDatabase db = new(b => b.AddJsonContext(FlagsKeyDictContext.Default));
        db.Table<FlagsKeyDictRow>().Schema.CreateTable();
        FlagsKeyDictRow row = new()
        {
            Id = 1,
            Shade = FlagsKeyAccess.Read,
            Map = new Dictionary<FlagsKeyAccess, int> { [FlagsKeyAccess.Read] = 1 },
        };
        db.Table<FlagsKeyDictRow>().Add(row);

        bool expected = row.Map.Keys.Contains(row.Shade);
        bool actual = db.Table<FlagsKeyDictRow>().Select(r => r.Map.Keys.Contains(r.Shade)).First();

        Assert.Equal(expected, actual);
    }
}
