using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JlEdgeDoubleRow
{
    [Key]
    public int Id { get; set; }

    public List<double> Numbers { get; set; } = [];
}

file sealed class JlEdgePersonRow
{
    [Key]
    public int Id { get; set; }

    public List<Person> People { get; set; } = [];
}

public class JsonListOrderingMaterializationParityTests
{
    private static TestDatabase DoubleDb()
    {
        return new TestDatabase(b => b.TypeConverters[typeof(List<double>)] =
            new SQLiteJsonConverter<List<double>>(TestJsonContext.Default.ListDouble));
    }

    private static TestDatabase PersonDb()
    {
        return new TestDatabase(b => b.TypeConverters[typeof(List<Person>)] =
            new SQLiteJsonConverter<List<Person>>(TestJsonContext.Default.ListPerson));
    }

    [Fact]
    public void OrderByStableTies()
    {
        using TestDatabase db = PersonDb();
        db.Table<JlEdgePersonRow>().Schema.CreateTable();
        List<Person> seed = new()
        {
            new Person { Name = "b", Home = new Address { City = "p0" } },
            new Person { Name = "a", Home = new Address { City = "p1" } },
            new Person { Name = "a", Home = new Address { City = "p2" } },
            new Person { Name = "b", Home = new Address { City = "p3" } },
            new Person { Name = "a", Home = new Address { City = "p4" } },
        };
        db.Table<JlEdgePersonRow>().Add(new JlEdgePersonRow { Id = 1, People = seed });

        List<string> expected = seed.OrderBy(p => p.Name).Select(p => p.Home.City).ToList();
        List<Person> got = db.Table<JlEdgePersonRow>()
            .Select(r => r.People.OrderBy(p => p.Name).ToList())
            .First();
        List<string> actual = got.Select(p => p.Home.City).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByDescendingStableTies()
    {
        using TestDatabase db = PersonDb();
        db.Table<JlEdgePersonRow>().Schema.CreateTable();
        List<Person> seed = new()
        {
            new Person { Name = "b", Home = new Address { City = "p0" } },
            new Person { Name = "a", Home = new Address { City = "p1" } },
            new Person { Name = "a", Home = new Address { City = "p2" } },
            new Person { Name = "b", Home = new Address { City = "p3" } },
            new Person { Name = "a", Home = new Address { City = "p4" } },
        };
        db.Table<JlEdgePersonRow>().Add(new JlEdgePersonRow { Id = 1, People = seed });

        List<string> expected = seed.OrderByDescending(p => p.Name).Select(p => p.Home.City).ToList();
        List<Person> got = db.Table<JlEdgePersonRow>()
            .Select(r => r.People.OrderByDescending(p => p.Name).ToList())
            .First();
        List<string> actual = got.Select(p => p.Home.City).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByThenReverseTies()
    {
        using TestDatabase db = PersonDb();
        db.Table<JlEdgePersonRow>().Schema.CreateTable();
        List<Person> seed = new()
        {
            new Person { Name = "b", Home = new Address { City = "p0" } },
            new Person { Name = "a", Home = new Address { City = "p1" } },
            new Person { Name = "a", Home = new Address { City = "p2" } },
            new Person { Name = "b", Home = new Address { City = "p3" } },
        };
        db.Table<JlEdgePersonRow>().Add(new JlEdgePersonRow { Id = 1, People = seed });

        List<string> expected = seed.OrderByDescending(p => p.Name).Select(p => p.Home.City).ToList();
        List<Person> got = db.Table<JlEdgePersonRow>()
            .Select(r => r.People.OrderBy(p => p.Name).Reverse().ToList())
            .First();
        List<string> actual = got.Select(p => p.Home.City).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByKeyThenTakeKeepsTies()
    {
        using TestDatabase db = PersonDb();
        db.Table<JlEdgePersonRow>().Schema.CreateTable();
        List<Person> seed = new()
        {
            new Person { Name = "a", Home = new Address { City = "p0" } },
            new Person { Name = "a", Home = new Address { City = "p1" } },
            new Person { Name = "a", Home = new Address { City = "p2" } },
            new Person { Name = "b", Home = new Address { City = "p3" } },
        };
        db.Table<JlEdgePersonRow>().Add(new JlEdgePersonRow { Id = 1, People = seed });

        List<string> expected = seed.OrderBy(p => p.Name).Take(2).Select(p => p.Home.City).ToList();
        List<Person> got = db.Table<JlEdgePersonRow>()
            .Select(r => r.People.OrderBy(p => p.Name).Take(2).ToList())
            .First();
        List<string> actual = got.Select(p => p.Home.City).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DoubleReverseElementAt()
    {
        using TestDatabase db = DoubleDb();
        db.Table<JlEdgeDoubleRow>().Schema.CreateTable();
        List<double> seed = new() { 1.5, 2.5, 3.5, 4.5 };
        db.Table<JlEdgeDoubleRow>().Add(new JlEdgeDoubleRow { Id = 1, Numbers = seed });

        double expected = seed.Reverse<double>().ElementAt(1);
        double actual = db.Table<JlEdgeDoubleRow>().Select(r => r.Numbers.AsEnumerable().Reverse().ElementAt(1)).First();

        Assert.Equal(expected, actual);
    }
}
