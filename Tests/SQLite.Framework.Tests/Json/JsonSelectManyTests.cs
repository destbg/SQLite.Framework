using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JsonSmRow
{
    [Key]
    public int Id { get; set; }

    public List<PersonWithTags> People { get; set; } = [];
}

public class JsonSelectManyTests
{
    private static readonly PersonWithTags[] Seed =
    [
        new PersonWithTags { Name = "Alice", Tags = ["a", "b"] },
        new PersonWithTags { Name = "Bob", Tags = ["c"] },
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<PersonWithTags>)] =
                new SQLiteJsonConverter<List<PersonWithTags>>(TestJsonContext.Default.ListPersonWithTags);
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString);
        });
        db.Table<JsonSmRow>().Schema.CreateTable();
        db.Table<JsonSmRow>().Add(new JsonSmRow { Id = 1, People = Seed.ToList() });
        return db;
    }

    [Fact]
    public void SelectMany_Flatten_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<string> expected = Seed.SelectMany(p => p.Tags).ToList();
        List<string> actual = db.Table<JsonSmRow>().Select(r => r.People.SelectMany(p => p.Tags).ToList()).First();

        Assert.Equal(["a", "b", "c"], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectManyThenWhere_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<string> expected = Seed.SelectMany(p => p.Tags).Where(t => t != "a").ToList();
        List<string> actual = db.Table<JsonSmRow>()
            .Select(r => r.People.SelectMany(p => p.Tags).Where(t => t != "a").ToList())
            .First();

        Assert.Equal(["b", "c"], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectManyThenContains_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        bool expectedTrue = Seed.SelectMany(p => p.Tags).Contains("c");
        bool expectedFalse = Seed.SelectMany(p => p.Tags).Contains("z");
        bool actualTrue = db.Table<JsonSmRow>().Select(r => r.People.SelectMany(p => p.Tags).Contains("c")).First();
        bool actualFalse = db.Table<JsonSmRow>().Select(r => r.People.SelectMany(p => p.Tags).Contains("z")).First();

        Assert.True(expectedTrue);
        Assert.False(expectedFalse);
        Assert.Equal(expectedTrue, actualTrue);
        Assert.Equal(expectedFalse, actualFalse);
    }

    [Fact]
    public void SelectManyThenCount_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        int expectedAll = Seed.SelectMany(p => p.Tags).Count();
        int expectedFiltered = Seed.SelectMany(p => p.Tags).Count(t => t != "a");
        int actualAll = db.Table<JsonSmRow>().Select(r => r.People.SelectMany(p => p.Tags).Count()).First();
        int actualFiltered = db.Table<JsonSmRow>().Select(r => r.People.SelectMany(p => p.Tags).Count(t => t != "a")).First();

        Assert.Equal(3, expectedAll);
        Assert.Equal(2, expectedFiltered);
        Assert.Equal(expectedAll, actualAll);
        Assert.Equal(expectedFiltered, actualFiltered);
    }

    [Fact]
    public void SelectManyThenSelect_ProjectsFlattenedElement()
    {
        using TestDatabase db = CreateDb();

        List<string> expected = Seed.SelectMany(p => p.Tags).Select(t => t + "!").ToList();
        List<string> actual = db.Table<JsonSmRow>()
            .Select(r => r.People.SelectMany(p => p.Tags).Select(t => t + "!").ToList())
            .First();

        Assert.Equal(["a!", "b!", "c!"], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectManyThenWhereThenSelect_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<string> expected = Seed.SelectMany(p => p.Tags).Where(t => t != "a").Select(t => t.ToUpper()).ToList();
        List<string> actual = db.Table<JsonSmRow>()
            .Select(r => r.People.SelectMany(p => p.Tags).Where(t => t != "a").Select(t => t.ToUpper()).ToList())
            .First();

        Assert.Equal(["B", "C"], expected);
        Assert.Equal(expected, actual);
    }
}
