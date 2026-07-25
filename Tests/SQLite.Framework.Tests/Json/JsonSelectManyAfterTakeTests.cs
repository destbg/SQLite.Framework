using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JsmtRow
{
    [Key]
    public int Id { get; set; }

    public List<PersonWithTags> People { get; set; } = [];
}

public class JsonSelectManyAfterTakeTests
{
    private static readonly PersonWithTags[] Seed =
    [
        new PersonWithTags { Name = "Alice", Tags = ["a", "b"] },
        new PersonWithTags { Name = "Bob", Tags = ["c", "d"] },
        new PersonWithTags { Name = "Carol", Tags = ["e"] },
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
        db.Table<JsmtRow>().Schema.CreateTable();
        db.Table<JsmtRow>().Add(new JsmtRow { Id = 1, People = Seed.ToList() });
        return db;
    }

    [Fact]
    public void Take_ThenSelectMany_ReturnsTagsOfFirstNPersons()
    {
        using TestDatabase db = CreateDb();

        List<string> expected = Seed.Take(1).SelectMany(p => p.Tags).ToList();

        List<string> actual = db.Table<JsmtRow>()
            .Select(r => r.People.Take(1).SelectMany(p => p.Tags).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Skip_ThenSelectMany_ReturnsTagsOfPersonsAfterSkip()
    {
        using TestDatabase db = CreateDb();

        List<string> expected = Seed.Skip(1).SelectMany(p => p.Tags).ToList();

        List<string> actual = db.Table<JsmtRow>()
            .Select(r => r.People.Skip(1).SelectMany(p => p.Tags).ToList())
            .First();

        Assert.Equal(expected, actual);
    }
}
