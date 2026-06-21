using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JsonSelectorOverloadRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];

    public List<PersonWithTags> People { get; set; } = [];
}

public class JsonCollectionSelectorOverloadParityTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32);
            b.TypeConverters[typeof(List<string>)] = new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString);
            b.TypeConverters[typeof(List<PersonWithTags>)] = new SQLiteJsonConverter<List<PersonWithTags>>(TestJsonContext.Default.ListPersonWithTags);
        });
        db.Table<JsonSelectorOverloadRow>().Schema.CreateTable();
        db.Table<JsonSelectorOverloadRow>().Add(new JsonSelectorOverloadRow
        {
            Id = 1,
            Numbers = [1, 2, 3, 4],
            People =
            [
                new PersonWithTags { Name = "Alice", Tags = ["a", "b"] },
                new PersonWithTags { Name = "Bob", Tags = ["c"] }
            ]
        });
        return db;
    }

    [Fact]
    public void SelectMany_WithResultSelector_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<PersonWithTags> people =
        [
            new PersonWithTags { Name = "Alice", Tags = ["a", "b"] },
            new PersonWithTags { Name = "Bob", Tags = ["c"] }
        ];
        List<string> expected = people.SelectMany(p => p.Tags, (p, t) => p.Name + ":" + t).ToList();

        List<string> actual = db.Table<JsonSelectorOverloadRow>()
            .Select(r => r.People.SelectMany(p => p.Tags, (p, t) => p.Name + ":" + t).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupBy_WithElementSelector_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<int> numbers = [1, 2, 3, 4];
        List<int> expected = numbers.GroupBy(x => x % 2, x => x * 10).Select(g => g.Sum()).OrderBy(s => s).ToList();

        List<int> actual = db.Table<JsonSelectorOverloadRow>()
            .Select(r => r.Numbers.GroupBy(x => x % 2, x => x * 10).Select(g => g.Sum()).OrderBy(s => s).ToList())
            .First();

        Assert.Equal(expected, actual);
    }
}
