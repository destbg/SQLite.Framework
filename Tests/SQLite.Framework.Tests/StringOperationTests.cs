using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringOperationTests
{
    [Fact]
    public void StringLength()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Short", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Long Title", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "Test", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where book.Title.Length > 5
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(5, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE LENGTH(b0.BookTitle) > @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Single(results);
        Assert.Equal("Long Title", results[0].Title);
    }

    [Fact]
    public void StringConcat()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Test", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Another", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "Book", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query =
            from book in db.Table<Book>()
            where book.Title + " - Book" == "Test - Book"
            select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(" - Book", command.Parameters[0].Value);
        Assert.Equal("Test - Book", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle || @p0 = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Single(results);
    }

    [Fact]
    public void StringConcatMultiple()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Test", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Another", AuthorId = 1, Price = 15 }
        });

        IQueryable<string> query =
            from book in db.Table<Book>()
            select book.Title + " by " + "Author";

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(" by ", command.Parameters[0].Value);
        Assert.Equal("Author", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookTitle || @p0 || @p1 AS "8"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<string> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("Test by Author", results[0]);
        Assert.Equal("Another by Author", results[1]);
    }

    [Fact]
    public void StringIsNullOrEmpty()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Test", AuthorId = 1, Price = 15 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where string.IsNullOrEmpty(book.Title)
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookTitle IS NULL OR b0.BookTitle = '')
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void StringIsNullOrWhiteSpace()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "  ", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Test", AuthorId = 1, Price = 15 },
            new Book { Id = 4, Title = "", AuthorId = 2, Price = 25 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where string.IsNullOrWhiteSpace(book.Title)
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookTitle IS NULL OR TRIM(b0.BookTitle, ' ') = '')
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Id == 1);
        Assert.Contains(results, b => b.Id == 4);
    }

    [Fact]
    public void StringPadLeft()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title.PadLeft(10) == "    Test"
            select book
        ).ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(10, command.Parameters[0].Value);
        Assert.Equal(' ', command.Parameters[1].Value);
        Assert.Equal("    Test", command.Parameters[2].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (CASE WHEN LENGTH(b0.BookTitle) >= @p0 THEN b0.BookTitle ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB((@p0 - LENGTH(b0.BookTitle)) / 2 + (@p0 - LENGTH(b0.BookTitle)) % 2)), '00', @p1), 1, @p0 - LENGTH(b0.BookTitle)) || b0.BookTitle) END) = @p2
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringPadRight()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title.PadRight(10) == "Test    "
            select book
        ).ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(10, command.Parameters[0].Value);
        Assert.Equal(' ', command.Parameters[1].Value);
        Assert.Equal("Test    ", command.Parameters[2].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (CASE WHEN LENGTH(b0.BookTitle) >= @p0 THEN b0.BookTitle ELSE (b0.BookTitle || (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB((@p0 - LENGTH(b0.BookTitle)) / 2 + (@p0 - LENGTH(b0.BookTitle)) % 2)), '00', @p1), 1, @p0 - LENGTH(b0.BookTitle)))) END) = @p2
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringLengthInSelect()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test Book",
            AuthorId = 1,
            Price = 10
        });

        var query = db.Table<Book>()
            .Select(b => new { b.Title, b.Title.Length });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookTitle AS "Title",
                            LENGTH(b0.BookTitle) AS "Length"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        var result = query.First();

        Assert.Equal("Test Book", result.Title);
        Assert.Equal(9, result.Length);
    }

    [Fact]
    public void StringToUpperInSelect()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test Book",
            AuthorId = 1,
            Price = 10
        });

        IQueryable<string> query = db.Table<Book>()
            .Select(b => b.Title.ToUpper());

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT UPPER(b0.BookTitle) AS "5"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        string result = query.First();

        Assert.Equal("TEST BOOK", result);
    }

    [Fact]
    public void StringToLowerInSelect()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test Book",
            AuthorId = 1,
            Price = 10
        });

        IQueryable<string> query = db.Table<Book>()
            .Select(b => b.Title.ToLower());

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT LOWER(b0.BookTitle) AS "5"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        string result = query.First();

        Assert.Equal("test book", result);
    }

    [Fact]
    public void StringReplaceInSelect()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test Book",
            AuthorId = 1,
            Price = 10
        });

        IQueryable<string> query = db.Table<Book>()
            .Select(b => b.Title.Replace("Test", "Sample"));

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("Test", command.Parameters[0].Value);
        Assert.Equal("Sample", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT REPLACE(b0.BookTitle, @p0, @p1) AS "7"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        string result = query.First();

        Assert.Equal("Sample Book", result);
    }

    [Fact]
    public void StringSubstringInSelect()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test Book",
            AuthorId = 1,
            Price = 10
        });

        IQueryable<string> query = db.Table<Book>()
            .Select(b => b.Title.Substring(0, 4));

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(0, command.Parameters[0].Value);
        Assert.Equal(4, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT SUBSTR(b0.BookTitle, @p0 + 1, @p1) AS "7"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        string result = query.First();

        Assert.Equal("Test", result);
    }

    [Fact]
    public void StringNullCoalescingWithLength()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<BookWithNotes>()
            .Where(b => (b.Notes ?? "").Length > 0)
            .ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("", command.Parameters[0].Value);
        Assert.Equal(0, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.Id AS "Id",
                            b0.Title AS "Title",
                            b0.Notes AS "Notes"
                     FROM "BookWithNotes" AS b0
                     WHERE LENGTH(COALESCE(b0.Notes, @p0)) > @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringNullCoalescingWithStartsWith()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<BookWithNotes>()
            .Where(b => (b.Notes ?? "").StartsWith("Important"))
            .ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("", command.Parameters[0].Value);
        Assert.Equal("Important%", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.Id AS "Id",
                            b0.Title AS "Title",
                            b0.Notes AS "Notes"
                     FROM "BookWithNotes" AS b0
                     WHERE COALESCE(b0.Notes, @p1) LIKE @p2 ESCAPE '\'
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringNullCoalescingWithContains()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<BookWithNotes>()
            .Where(b => (b.Notes ?? "Unknown").Contains("test"))
            .ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("Unknown", command.Parameters[0].Value);
        Assert.Equal("%test%", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.Id AS "Id",
                            b0.Title AS "Title",
                            b0.Notes AS "Notes"
                     FROM "BookWithNotes" AS b0
                     WHERE COALESCE(b0.Notes, @p1) LIKE @p2 ESCAPE '\'
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringNullCoalescingInSelect()
    {
        using TestDatabase db = new();

        db.Table<BookWithNotes>().CreateTable();
        db.Table<BookWithNotes>().Add(new BookWithNotes
        {
            Id = 1,
            Title = "Test Book",
            Notes = null
        });
        db.Table<BookWithNotes>().Add(new BookWithNotes
        {
            Id = 2,
            Title = "Another Book",
            Notes = "Some notes"
        });

        var query = db.Table<BookWithNotes>()
            .Select(b => new { b.Id, NotesOrDefault = b.Notes ?? "No notes" });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("No notes", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.Id AS "Id",
                            COALESCE(b0.Notes, @p0) AS "NotesOrDefault"
                     FROM "BookWithNotes" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("No notes", results[0].NotesOrDefault);
        Assert.Equal("Some notes", results[1].NotesOrDefault);
    }

    [Fact]
    public void StringNullCoalescingWithToUpper()
    {
        using TestDatabase db = new();

        db.Table<BookWithNotes>().CreateTable();
        db.Table<BookWithNotes>().Add(new BookWithNotes
        {
            Id = 1,
            Title = "Test Book",
            Notes = null
        });

        IQueryable<string> query = db.Table<BookWithNotes>()
            .Select(b => (b.Notes ?? "default").ToUpper());

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("default", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT UPPER(COALESCE(b0.Notes, @p0)) AS "6"
                     FROM "BookWithNotes" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        string result = query.First();

        Assert.Equal("DEFAULT", result);
    }

    private class BookWithNotes
    {
        public int Id { get; init; }
        public required string Title { get; init; }
        public string? Notes { get; init; }
    }
}