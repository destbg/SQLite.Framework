using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class AnyContainsListContext : JsonSerializerContext;

internal sealed class AnyContainsRow
{
    [Key]
    public int Id { get; set; }

    public List<int> ItemsA { get; set; } = [];

    public List<int> ItemsB { get; set; } = [];
}

public class JsonListContainsInsideAnyPredicateTests
{
    private static List<AnyContainsRow> Rows() =>
    [
        new() { Id = 1, ItemsA = [1, 2, 3], ItemsB = [9] },
        new() { Id = 2, ItemsA = [4], ItemsB = [4, 7] },
        new() { Id = 3, ItemsA = [5, 6], ItemsB = [] },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.AddJsonContext(AnyContainsListContext.Default));
        db.Table<AnyContainsRow>().Schema.CreateTable();
        db.Table<AnyContainsRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void CapturedListAnyWithJsonContains()
    {
        using TestDatabase db = Seed();

        List<int> captured = [2, 7];
        List<int> expected = Rows()
            .Where(r => captured.Any(v => r.ItemsA.Contains(v)))
            .Select(r => r.Id)
            .ToList();
        Assert.Equal([1], expected);

        List<int> actual = db.Table<AnyContainsRow>()
            .Where(r => captured.Any(v => r.ItemsA.Contains(v)))
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JsonListAnyWithOtherJsonListContains()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows()
            .Where(r => r.ItemsA.Any(v => r.ItemsB.Contains(v)))
            .Select(r => r.Id)
            .ToList();
        Assert.Equal([2], expected);

        List<int> actual = db.Table<AnyContainsRow>()
            .Where(r => r.ItemsA.Any(v => r.ItemsB.Contains(v)))
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();
        Assert.Equal(expected, actual);
    }
}
