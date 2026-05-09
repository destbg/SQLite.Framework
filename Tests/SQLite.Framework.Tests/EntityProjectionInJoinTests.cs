using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EntityProjectionInJoinTests
{
    [Fact]
    public void Join_SelectAnonymousWithEntityValues_GeneratesPrefixedColumns()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            select new { Book = book, Author = author }
        ).ToSqlCommand();

        Assert.Contains("\"Book.Id\"", command.CommandText);
        Assert.Contains("\"Book.Title\"", command.CommandText);
        Assert.Contains("\"Author.Id\"", command.CommandText);
        Assert.Contains("\"Author.Name\"", command.CommandText);
    }

    [Fact]
    public void Join_SelectAnonymousWithEntityValues_MaterializesEachEntity()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Author>();
        db.Schema.CreateTable<Book>();

        Author author = new() { Name = "Sandi Metz", Email = "sandi@example.com", BirthDate = new DateTime(1964, 1, 1) };
        db.Table<Author>().Add(author);

        Book book = new() { Title = "Practical Object-Oriented Design", AuthorId = author.Id, Price = 29.99 };
        db.Table<Book>().Add(book);

        var rows = (
            from b in db.Table<Book>()
            join a in db.Table<Author>() on b.AuthorId equals a.Id
            orderby b.Title
            select new { Book = b, Author = a }
        ).ToList();

        var row = Assert.Single(rows);
        Assert.NotNull(row.Book);
        Assert.NotNull(row.Author);
        Assert.Equal("Practical Object-Oriented Design", row.Book.Title);
        Assert.Equal(29.99, row.Book.Price);
        Assert.Equal(author.Id, row.Book.AuthorId);
        Assert.Equal("Sandi Metz", row.Author.Name);
        Assert.Equal("sandi@example.com", row.Author.Email);
        Assert.Equal(author.Id, row.Author.Id);
    }

    [Fact]
    public void LeftJoin_SelectAnonymousWithEntityValues_NullsRightEntityWhenNoMatch()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Author>();
        db.Schema.CreateTable<Book>();

        Book orphan = new() { Title = "Orphan Book", AuthorId = 999, Price = 9.99 };
        db.Table<Book>().Add(orphan);

        var rows = (
            from b in db.Table<Book>()
            join a in db.Table<Author>() on b.AuthorId equals a.Id into ag
            from a in ag.DefaultIfEmpty()
            select new { Book = b, Author = a }
        ).ToList();

        var row = Assert.Single(rows);
        Assert.Equal("Orphan Book", row.Book.Title);
        Assert.Equal(999, row.Book.AuthorId);
        Assert.Equal(0, row.Author.Id);
        Assert.Null(row.Author.Name);
    }

    [Fact]
    public void Join_SelectAnonymousWithEntityAndScalar_MaterializesBoth()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Author>();
        db.Schema.CreateTable<Book>();

        Author author = new() { Name = "Kent Beck", Email = "kent@example.com", BirthDate = new DateTime(1961, 3, 31) };
        db.Table<Author>().Add(author);

        Book book = new() { Title = "TDD", AuthorId = author.Id, Price = 35.0 };
        db.Table<Book>().Add(book);

        var rows = (
            from b in db.Table<Book>()
            join a in db.Table<Author>() on b.AuthorId equals a.Id
            select new { Book = b, AuthorName = a.Name }
        ).ToList();

        var row = Assert.Single(rows);
        Assert.Equal("TDD", row.Book.Title);
        Assert.Equal("Kent Beck", row.AuthorName);
    }
}
