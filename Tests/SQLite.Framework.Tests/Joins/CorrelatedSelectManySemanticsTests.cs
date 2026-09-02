using System;
using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CorrelatedSelectManySemanticsTests
{
    [Fact]
    public void CorrelatedSelectMany_InnerSourceFilter_MatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        Author[] authors =
        [
            new Author { Id = 1, Name = "A1", Email = "a1", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "A2", Email = "a2", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 3, Name = "A3", Email = "a3", BirthDate = new DateTime(2000, 1, 1) }
        ];
        Book[] books =
        [
            new Book { Id = 10, Title = "b1", AuthorId = 1, Price = 1 },
            new Book { Id = 11, Title = "b2", AuthorId = 1, Price = 2 },
            new Book { Id = 12, Title = "b3", AuthorId = 2, Price = 0 },
            new Book { Id = 13, Title = "b4", AuthorId = 4, Price = 3 }
        ];
        db.Table<Author>().AddRange(authors);
        db.Table<Book>().AddRange(books);

        List<string> expected = (
                from a in authors
                from b in books.Where(b => b.AuthorId == a.Id && b.Price > 0)
                orderby a.Id, b.Id
                select a.Name + ":" + b.Title)
            .ToList();
        List<string> actual = (
                from a in db.Table<Author>()
                from b in db.Table<Book>().Where(b => b.AuthorId == a.Id && b.Price > 0)
                orderby a.Id, b.Id
                select a.Name + ":" + b.Title)
            .ToList();

        Assert.Equal(["A1:b1", "A1:b2"], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CorrelatedSelectMany_InnerSourceDefaultIfEmpty_MatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        Author[] authors =
        [
            new Author { Id = 1, Name = "A1", Email = "a1", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "A2", Email = "a2", BirthDate = new DateTime(2000, 1, 1) }
        ];
        Book[] books =
        [
            new Book { Id = 10, Title = "b1", AuthorId = 1, Price = 1 }
        ];
        db.Table<Author>().AddRange(authors);
        db.Table<Book>().AddRange(books);

        List<string> expected = (
                from a in authors
                from b in books.Where(b => b.AuthorId == a.Id).DefaultIfEmpty()
                orderby a.Id
                select a.Name + ":" + (b == null ? "none" : b.Title))
            .ToList();
        List<string> actual = (
                from a in db.Table<Author>()
                from b in db.Table<Book>().Where(b => b.AuthorId == a.Id).DefaultIfEmpty()
                orderby a.Id
                select a.Name + ":" + (b == null ? "none" : b.Title))
            .ToList();

        Assert.Equal(["A1:b1", "A2:none"], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CorrelatedSelectMany_OptionalWholeRow_MatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        Author[] authors =
        [
            new Author { Id = 1, Name = "A1", Email = "a1", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "A2", Email = "a2", BirthDate = new DateTime(2000, 1, 1) }
        ];
        Book[] books =
        [
            new Book { Id = 10, Title = "b1", AuthorId = 1, Price = 0 }
        ];
        db.Table<Author>().AddRange(authors);
        db.Table<Book>().AddRange(books);

        List<int?> expected = (
                from a in authors
                from b in books.Where(b => b.AuthorId == a.Id).DefaultIfEmpty()
                orderby a.Id
                select b)
            .Select(book => book?.Id)
            .ToList();
        List<int?> actual = (
                from a in db.Table<Author>()
                from b in db.Table<Book>().Where(b => b.AuthorId == a.Id).DefaultIfEmpty()
                orderby a.Id
                select b)
            .AsEnumerable()
            .Select(book => book?.Id)
            .ToList();

        Assert.Equal([10, null], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CorrelatedSelectMany_UntranslatablePredicate_IsRejected()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => (
                from a in db.Table<Author>()
                from b in db.Table<Book>().Where(b => b.AuthorId == a.Id && CmcClientFns.Tag(b.Title) == "[x]")
                select b.Id)
            .ToList());

        Assert.Contains("Unsupported WHERE expression", exception.Message);
    }
}
