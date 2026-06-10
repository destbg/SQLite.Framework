using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DefaultIfEmptyPrefixedRangeVariableTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Publisher>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "ann", Email = "a@x", BirthDate = new DateTime(2000, 1, 1) });
        db.Table<Publisher>().Add(new Publisher { Id = 1, Name = "pub", Type = PublisherType.Book });
        db.Table<Book>().Add(new Book { Id = 1, Title = "t1", AuthorId = 1, Price = 5 });
        return db;
    }

    [Fact]
    public void DefaultIfEmpty_RangeVariablesSharingGroupNamePrefix_TranslatesAndRuns()
    {
        using TestDatabase db = Seed();

        var results = (
            from ga in db.Table<Author>()
            from gb in db.Table<Publisher>()
            join c in db.Table<Book>() on ga.Id equals c.AuthorId into g
            from c in g.DefaultIfEmpty()
            select new { AuthorName = ga.Name, PublisherName = gb.Name, BookTitle = c.Title }
        ).ToList();

        Assert.Single(results);
        Assert.Equal("ann", results[0].AuthorName);
        Assert.Equal("pub", results[0].PublisherName);
        Assert.Equal("t1", results[0].BookTitle);
    }
}
