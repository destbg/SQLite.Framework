using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

internal sealed class JsonPeopleRow
{
    [Key]
    public int Id { get; set; }

    public List<PersonWithTags> People { get; set; } = [];
}

public class JsonCollectionBugTests
{
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
