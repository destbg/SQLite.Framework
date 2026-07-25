using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonMixedListRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];

    public List<string> Words { get; set; } = [];
}

public class JsonSelectUntranslatedMethodTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32);
            b.TypeConverters[typeof(List<string>)] = new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString);
        });
        db.Table<JsonMixedListRow>().Schema.CreateTable();
        db.Table<JsonMixedListRow>().Add(new JsonMixedListRow { Id = 1, Numbers = [3, 1, 2], Words = ["b", "a", "c"] });
        return db;
    }

    [Fact]
    public void ElementAtOrDefaultInSelectReturnsTheElement()
    {
        using TestDatabase db = SetupDatabase();

        string? expected = new List<string> { "b", "a", "c" }.ElementAtOrDefault(1);

        Assert.Equal("a", expected);

        string? actual = db.Table<JsonMixedListRow>()
            .Select(r => r.Words.ElementAtOrDefault(1))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AppendInSelectReturnsTheExtendedList()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = new List<int> { 3, 1, 2 }.Append(9).ToList();

        Assert.Equal([3, 1, 2, 9], expected);

        List<int> actual = db.Table<JsonMixedListRow>()
            .Select(r => r.Numbers.Append(9).ToList())
            .First();

        Assert.Equal(expected, actual);
    }
}
