using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class WeirdIndexRow
{
    [Key]
    public int Id { get; set; }

    [Indexed(Name = "weird\"name")]
    public string Val { get; set; } = "";
}

public class JoinIdentifierTests
{
    [Fact]
    public void CrossJoinWholeEntityProjectionDoesNotPullSiblingColumns()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "x", Email = "x", BirthDate = DateTime.UnixEpoch });
        db.Table<Author>().Add(new Author { Id = 2, Name = "y", Email = "y", BirthDate = DateTime.UnixEpoch });
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 2, Price = 2 });

        List<Author> authors = (from a in db.Table<Author>()
                                from ab in db.Table<Book>()
                                select a).Distinct().ToList();

        Assert.Equal(2, authors.Count);
    }

    [Fact]
    public void IndexNameWithQuoteIsEscaped()
    {
        using TestDatabase db = new();

        Exception? ex = Record.Exception(() => db.Table<WeirdIndexRow>().Schema.CreateTable());

        Assert.Null(ex);
    }

    [Fact]
    public void UpdateFromCrossJoinDoesNotThrowNullReference()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Author>().Add(new Author { Id = 1, Name = "n", Email = "e", BirthDate = DateTime.UnixEpoch });

        Exception? ex = Record.Exception(() =>
            db.Table<Book>()
                .SelectMany(_ => db.Table<Author>(), (b, a) => new { b, a })
                .ExecuteUpdate(s => s.Set(x => x.b.Title, x => x.a.Name)));

        Assert.False(ex is NullReferenceException, ex?.ToString());
    }
}
