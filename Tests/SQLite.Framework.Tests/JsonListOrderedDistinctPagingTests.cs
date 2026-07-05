using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<string>))]
internal partial class OrderedDistinctListContext : JsonSerializerContext;

internal sealed class OrderedDistinctListRow
{
    [Key]
    public int Id { get; set; }

    public List<string> Tags { get; set; } = [];
}

public class JsonListOrderedDistinctPagingTests
{
    private static TestDatabase Seed(out List<string> local)
    {
        local = ["b", "a", "b", "a", "c"];
        TestDatabase db = new(b => b.AddJsonContext(OrderedDistinctListContext.Default));
        db.Table<OrderedDistinctListRow>().Schema.CreateTable();
        db.Table<OrderedDistinctListRow>().Add(new OrderedDistinctListRow { Id = 1, Tags = ["b", "a", "b", "a", "c"] });
        return db;
    }

    [Fact]
    public void OrderByDistinctTakeMatchesLinqToObjects()
    {
        using TestDatabase db = Seed(out List<string> local);

        List<string> expected = local.OrderBy(x => x).Distinct().Take(2).ToList();
        List<string> actual = db.Table<OrderedDistinctListRow>().Select(r => r.Tags.OrderBy(x => x).Distinct().Take(2).ToList()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByDistinctSkipMatchesLinqToObjects()
    {
        using TestDatabase db = Seed(out List<string> local);

        List<string> expected = local.OrderBy(x => x).Distinct().Skip(1).ToList();
        List<string> actual = db.Table<OrderedDistinctListRow>().Select(r => r.Tags.OrderBy(x => x).Distinct().Skip(1).ToList()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByDescendingDistinctTakeMatchesLinqToObjects()
    {
        using TestDatabase db = Seed(out List<string> local);

        List<string> expected = local.OrderByDescending(x => x).Distinct().Take(2).ToList();
        List<string> actual = db.Table<OrderedDistinctListRow>().Select(r => r.Tags.OrderByDescending(x => x).Distinct().Take(2).ToList()).First();

        Assert.Equal(expected, actual);
    }
}
