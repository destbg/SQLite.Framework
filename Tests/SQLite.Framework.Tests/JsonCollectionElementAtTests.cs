using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JsonElementAtRow
{
    [Key]
    public int Id { get; set; }

    public List<string> Tags { get; set; } = [];
}

public class JsonCollectionElementAtTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
        db.Table<JsonElementAtRow>().Schema.CreateTable();
        db.Table<JsonElementAtRow>().Add(new JsonElementAtRow { Id = 1, Tags = ["a", "b"] });
        return db;
    }

    [Fact]
    public void ElementAt_InRange_ReturnsElement()
    {
        using TestDatabase db = CreateDb();

        string expected = new List<string> { "a", "b" }.ElementAt(1);
        string actual = db.Table<JsonElementAtRow>().Select(r => r.Tags.ElementAt(1)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ElementAt_PastEnd_ReturnsDefault()
    {
        using TestDatabase db = CreateDb();

        string? actual = db.Table<JsonElementAtRow>().Select(r => r.Tags.ElementAt(5)).First();

        Assert.Null(actual);
    }
}
