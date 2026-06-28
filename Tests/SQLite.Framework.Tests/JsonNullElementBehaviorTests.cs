using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JsonNullElementRow
{
    [Key]
    public int Id { get; set; }

    public List<string> Tags { get; set; } = [];
}

public class JsonNullElementBehaviorTests
{
    private static TestDatabase CreateDb(List<string> tags)
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
        db.Table<JsonNullElementRow>().Schema.CreateTable();
        db.Table<JsonNullElementRow>().Add(new JsonNullElementRow { Id = 1, Tags = tags });
        return db;
    }

    [Fact]
    public void All_EqualityOverNullElement_MatchesDotNet()
    {
        List<string> tags = ["a", null!];
        using TestDatabase db = CreateDb(tags);

        bool expected = new List<string> { "a", null! }.All(x => x == "a");
        bool actual = db.Table<JsonNullElementRow>().Select(r => r.Tags.All(x => x == "a")).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Except_SecondOperandWithNull_RemovesMatchingElements()
    {
        List<string> tags = ["a", "b", "c"];
        using TestDatabase db = CreateDb(tags);
        List<string> other = ["b", null!];

        List<string> expected = new List<string> { "a", "b", "c" }.Except(other).OrderBy(x => x).ToList();
        List<string> actual = db.Table<JsonNullElementRow>().Select(r => r.Tags.Except(other).ToList()).First();

        Assert.Equal(expected, actual.OrderBy(x => x).ToList());
    }

    [Fact]
    public void Intersect_SecondOperandWithNull_DropsNull()
    {
        List<string> tags = ["a", null!, "c"];
        using TestDatabase db = CreateDb(tags);
        List<string> other = [null!, "c"];

        List<string> actual = db.Table<JsonNullElementRow>().Select(r => r.Tags.Intersect(other).ToList()).First();

        Assert.Equal(["c"], actual);
    }

    [Fact]
    public void DistinctCount_OverNullElement_ExcludesNull()
    {
        List<string> tags = [null!, "a", "a"];
        using TestDatabase db = CreateDb(tags);

        int actual = db.Table<JsonNullElementRow>().Select(r => r.Tags.Distinct().Count()).First();

        Assert.Equal(1, actual);
    }
}
