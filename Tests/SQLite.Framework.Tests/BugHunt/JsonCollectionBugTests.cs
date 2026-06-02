using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

internal sealed class JsonTagRow
{
    [Key]
    public int Id { get; set; }

    public List<string> Tags { get; set; } = [];
}

internal sealed class JsonNumberRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

internal sealed class JsonPeopleRow
{
    [Key]
    public int Id { get; set; }

    public List<PersonWithTags> People { get; set; } = [];
}

public class JsonCollectionBugTests
{
    [Fact]
    public void ElementAtNegativeIndex_ThrowsLikeLinqToObjects()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
        db.Table<JsonTagRow>().Schema.CreateTable();
        db.Table<JsonTagRow>().Add(new JsonTagRow { Id = 1, Tags = ["a", "b", "c"] });

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Table<JsonTagRow>().Select(r => r.Tags.ElementAt(-1)).First());
    }

    [Fact]
    public void SelectProjectionThenWhere_MatchesLinqToObjects()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonNumberRow>().Schema.CreateTable();
        db.Table<JsonNumberRow>().Add(new JsonNumberRow { Id = 1, Numbers = [1, 2, 3] });

        List<int> expected = new[] { 1, 2, 3 }.Select(x => x * 2).Where(v => v > 5).ToList();
        List<int> actual = db.Table<JsonNumberRow>()
            .Select(r => r.Numbers.Select(x => x * 2).Where(v => v > 5).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectProjectionThenContains_MatchesLinqToObjects()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonNumberRow>().Schema.CreateTable();
        db.Table<JsonNumberRow>().Add(new JsonNumberRow { Id = 1, Numbers = [1, 2, 3] });

        bool expected = new[] { 1, 2, 3 }.Select(x => x * 2).Contains(6);
        bool actual = db.Table<JsonNumberRow>()
            .Select(r => r.Numbers.Select(x => x * 2).Contains(6))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectManyThenWhere_MatchesLinqToObjects()
    {
        using TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<PersonWithTags>)] =
                new SQLiteJsonConverter<List<PersonWithTags>>(TestJsonContext.Default.ListPersonWithTags);
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString);
        });
        db.Table<JsonPeopleRow>().Schema.CreateTable();
        db.Table<JsonPeopleRow>().Add(new JsonPeopleRow
        {
            Id = 1,
            People =
            [
                new PersonWithTags { Name = "Alice", Tags = ["a", "b"] },
                new PersonWithTags { Name = "Bob", Tags = ["c"] },
            ],
        });

        string[] expectedSource = ["a", "b", "c"];
        List<string> expected = expectedSource.Where(t => t != "a").ToList();
        List<string> actual = db.Table<JsonPeopleRow>()
            .Select(r => r.People.SelectMany(p => p.Tags).Where(t => t != "a").ToList())
            .First();

        Assert.Equal(expected, actual);
    }
}
