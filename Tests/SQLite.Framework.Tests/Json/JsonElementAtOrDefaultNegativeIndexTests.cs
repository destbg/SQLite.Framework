using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JsonElementAtOrDefaultRow
{
    [Key]
    public int Id { get; set; }

    public List<string> Tags { get; set; } = [];
}

public class JsonElementAtOrDefaultNegativeIndexTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
        db.Table<JsonElementAtOrDefaultRow>().Schema.CreateTable();
        db.Table<JsonElementAtOrDefaultRow>().Add(new JsonElementAtOrDefaultRow { Id = 1, Tags = ["a", "b"] });
        return db;
    }

    [Fact]
    public void ElementAtOrDefault_NegativeIndex_ReturnsDefaultLikeLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        string? expected = new List<string> { "a", "b" }.ElementAtOrDefault(-1);
        Assert.Null(expected);

        string? actual = db.Table<JsonElementAtOrDefaultRow>().Select(r => r.Tags.ElementAtOrDefault(-1)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ElementAtOrDefault_PastEnd_ReturnsDefaultLikeLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        string? expected = new List<string> { "a", "b" }.ElementAtOrDefault(5);
        Assert.Null(expected);

        string? actual = db.Table<JsonElementAtOrDefaultRow>().Select(r => r.Tags.ElementAtOrDefault(5)).First();

        Assert.Equal(expected, actual);
    }
}
