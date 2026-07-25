using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CrossFromProjectionParityTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "ann", Email = "e", BirthDate = new DateTime(2000, 1, 1) });
        db.Table<Book>().Add(new Book { Id = 1, Title = "t1", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "t2", AuthorId = 1, Price = 2 });
        return db;
    }

    [Fact]
    public void QuerySyntax_TwoFroms_DirectAnonymousSelect_Works()
    {
        using TestDatabase db = Seed();

        var results = (
            from b in db.Table<Book>()
            from a in db.Table<Author>()
            select new { b.Title, AuthorName = a.Name }
        ).OrderBy(x => x.Title).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("t1", results[0].Title);
        Assert.Equal("ann", results[0].AuthorName);
    }

    [Fact]
    public void Fluent_SelectMany_AnonymousResultSelector_Works()
    {
        using TestDatabase db = Seed();

        var results = db.Table<Book>()
            .SelectMany(b => db.Table<Author>(), (b, a) => new { b.Title, AuthorName = a.Name })
            .OrderBy(x => x.Title)
            .ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("t2", results[1].Title);
        Assert.Equal("ann", results[1].AuthorName);
    }
}
