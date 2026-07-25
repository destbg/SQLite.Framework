using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class LongCountListContext : JsonSerializerContext;

internal sealed class LongCountRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Items { get; set; } = [];
}

public class JsonListLongCountTests
{
    [Fact]
    public void LongCountReturnsElementCount()
    {
        using TestDatabase db = new(b => b.AddJsonContext(LongCountListContext.Default));
        db.Table<LongCountRow>().Schema.CreateTable();
        db.Table<LongCountRow>().Add(new LongCountRow { Id = 1, Items = [1, 2, 3] });

        long expected = new List<int> { 1, 2, 3 }.LongCount();
        Assert.Equal(3L, expected);

        long actual = db.Table<LongCountRow>().Select(r => r.Items.LongCount()).First();
        Assert.Equal(expected, actual);
    }
}
