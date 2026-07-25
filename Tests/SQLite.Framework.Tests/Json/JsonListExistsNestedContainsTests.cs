using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class ExistsNestedListContext : JsonSerializerContext;

internal sealed class ExistsNestedRow
{
    [Key]
    public int Id { get; set; }

    public List<int> ItemsA { get; set; } = [];

    public List<int> ItemsB { get; set; } = [];
}

public class JsonListExistsNestedContainsTests
{
    private static List<ExistsNestedRow> Rows() =>
    [
        new() { Id = 1, ItemsA = [1, 2, 3], ItemsB = [9] },
        new() { Id = 2, ItemsA = [4], ItemsB = [4, 7] },
        new() { Id = 3, ItemsA = [5, 6], ItemsB = [] },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.AddJsonContext(ExistsNestedListContext.Default));
        db.Table<ExistsNestedRow>().Schema.CreateTable();
        db.Table<ExistsNestedRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ExistsWithOtherJsonListContains()
    {
        using TestDatabase db = Seed();
        List<ExistsNestedRow> rows = Rows();

        List<int> expected = rows.Where(r => r.ItemsA.Exists(v => r.ItemsB.Contains(v))).Select(r => r.Id).ToList();
        List<int> actual = db.Table<ExistsNestedRow>().Where(r => r.ItemsA.Exists(v => r.ItemsB.Contains(v))).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrueForAllWithOtherJsonListContains()
    {
        using TestDatabase db = Seed();
        List<ExistsNestedRow> rows = Rows();

        List<int> expected = rows.Where(r => r.ItemsA.TrueForAll(v => !r.ItemsB.Contains(v))).Select(r => r.Id).ToList();
        List<int> actual = db.Table<ExistsNestedRow>().Where(r => r.ItemsA.TrueForAll(v => !r.ItemsB.Contains(v))).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }
}
