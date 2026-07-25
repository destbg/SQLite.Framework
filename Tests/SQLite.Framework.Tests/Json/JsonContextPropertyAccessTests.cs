using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonContextPropertyAccessTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b => b.AddJsonContext(PersonRootJsonContext.Default));
        db.Table<PersonEntity>().Schema.CreateTable();
        db.Table<PersonEntity>().Add(new PersonEntity
        {
            Id = 1,
            Person = new Person
            {
                Name = "Alice",
                Home = new Address { Street = "1 Oak", City = "Shelbyville" }
            }
        });
        return db;
    }

    [Fact]
    public void WherePropertyOnJsonContextColumnFilters()
    {
        using TestDatabase db = SetupDatabase();

        List<PersonEntity> rows = db.Table<PersonEntity>()
            .Where(p => p.Person.Name == "Alice")
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void OrderByPropertyOnJsonContextColumnSorts()
    {
        using TestDatabase db = SetupDatabase();

        List<PersonEntity> rows = db.Table<PersonEntity>()
            .OrderBy(p => p.Person.Name)
            .ToList();

        Assert.Single(rows);
    }
}
