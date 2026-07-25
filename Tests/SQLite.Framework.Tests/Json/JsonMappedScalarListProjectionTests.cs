using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JsonMappedListRow
{
    [Key]
    public int Id { get; set; }

    public List<Person> People { get; set; } = [];
}

public class JsonMappedScalarListProjectionTests
{
    [Fact]
    public void OrderByThenSelectScalarToList_MatchesObjects()
    {
        using TestDatabase db = Db();
        List<Person> people = Seed(db);

        List<string> expected = people.OrderBy(p => p.Name).Select(p => p.Name).ToList();
        List<string> actual = db.Table<JsonMappedListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.People.OrderBy(p => p.Name).Select(p => p.Name).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectScalarToList_MatchesObjects()
    {
        using TestDatabase db = Db();
        List<Person> people = Seed(db);

        List<string> expected = people.Select(p => p.Name).ToList();
        List<string> actual = db.Table<JsonMappedListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.People.Select(p => p.Name).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereThenSelectScalarToList_MatchesObjects()
    {
        using TestDatabase db = Db();
        List<Person> people = Seed(db);

        List<string> expected = people.Where(p => p.Name != "b").Select(p => p.Name).OrderBy(s => s).ToList();
        List<string> actual = db.Table<JsonMappedListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.People.Where(p => p.Name != "b").Select(p => p.Name).ToList())
            .First()
            .OrderBy(s => s)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase Db()
    {
        return new TestDatabase(b =>
        {
            b.TypeConverters[typeof(List<Person>)] = new SQLiteJsonConverter<List<Person>>(TestJsonContext.Default.ListPerson);
            b.TypeConverters[typeof(List<string>)] = new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString);
        });
    }

    private static List<Person> Seed(TestDatabase db)
    {
        db.Table<JsonMappedListRow>().Schema.CreateTable();
        List<Person> people = new()
        {
            new Person { Name = "c" },
            new Person { Name = "a" },
            new Person { Name = "b" },
        };
        db.Table<JsonMappedListRow>().Add(new JsonMappedListRow { Id = 1, People = people });
        return people;
    }
}
