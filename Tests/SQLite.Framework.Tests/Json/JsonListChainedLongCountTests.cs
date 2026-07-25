using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class ChainedLongCountListContext : JsonSerializerContext;

internal sealed class ChainedLongCountRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Items { get; set; } = [];
}

public class JsonListChainedLongCountTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.AddJsonContext(ChainedLongCountListContext.Default));
        db.Table<ChainedLongCountRow>().Schema.CreateTable();
        db.Table<ChainedLongCountRow>().Add(new ChainedLongCountRow { Id = 1, Items = [1, 2, 3, 4] });
        return db;
    }

    [Fact]
    public void WhereThenLongCount()
    {
        using TestDatabase db = Seed();

        long expected = new List<int> { 1, 2, 3, 4 }.Where(v => v > 2).LongCount();
        Assert.Equal(2L, expected);

        long actual = db.Table<ChainedLongCountRow>().Select(r => r.Items.Where(v => v > 2).LongCount()).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongCountWithPredicate()
    {
        using TestDatabase db = Seed();

        long expected = new List<int> { 1, 2, 3, 4 }.LongCount(v => v > 2);
        Assert.Equal(2L, expected);

        long actual = db.Table<ChainedLongCountRow>().Select(r => r.Items.LongCount(v => v > 2)).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TakeThenLongCount()
    {
        using TestDatabase db = Seed();

        long expected = new List<int> { 1, 2, 3, 4 }.Take(3).LongCount();
        Assert.Equal(3L, expected);

        long actual = db.Table<ChainedLongCountRow>().Select(r => r.Items.Take(3).LongCount()).First();
        Assert.Equal(expected, actual);
    }
}
