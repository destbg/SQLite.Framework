using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

// ReSharper disable AccessToDisposedClosure
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace SQLite.Framework.Tests;

public class MethodCallTests
{
    [Fact]
    public void ListMax()
    {
        using TestDatabase db = new();

        List<int> list = [1, 2, 3];

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id == list.Max()
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(3, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ListContains()
    {
        using TestDatabase db = new();

        List<int> list = [1, 2, 3];

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where list.Contains(book.Id)
            select book
        ).ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal(3, command.Parameters[2].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId IN (@p1, @p2, @p3)
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void QueryableContains()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where (
                from b in db.Table<Book>()
                where b.Title == "test"
                select b.Title
            ).Contains("test")
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("test", command.Parameters[0].Value);
        Assert.Equal("test", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE @p2 IN (
                         SELECT b1.BookTitle AS "Title"
                         FROM "Books" AS b1
                         WHERE b1.BookTitle = @p0
                     )
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void QueryableMax()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id == (
                from b in db.Table<Book>()
                where b.Title == "test"
                select b.Id
            ).Max()
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("test", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId = (
                         SELECT MAX(b1.BookId) AS "11"
                         FROM "Books" AS b1
                         WHERE b1.BookTitle = @p0
                     )
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringContains()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "My test book", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Another book", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "testing 123", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where book.Title.Contains("test")
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("%test%", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle LIKE @p1 ESCAPE '\'
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Title == "My test book");
        Assert.Contains(results, b => b.Title == "testing 123");
    }

    [Fact]
    public void StringContainsItself()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "My test book", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Another book", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "testing 123", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where book.Title.Contains(book.Title)
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle LIKE '%'||b0.BookTitle||'%' ESCAPE '\'
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void StringContainsComparison()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title.Contains("test", StringComparison.OrdinalIgnoreCase)
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("%test%", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle LIKE @p2 ESCAPE '\' COLLATE NOCASE
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringStartsWith()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "test book", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Another test", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "testing", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where book.Title.StartsWith("test")
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("test%", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle LIKE @p1 ESCAPE '\'
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Title == "test book");
        Assert.Contains(results, b => b.Title == "testing");
    }

    [Fact]
    public void StringEndsWith()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title.EndsWith("test%")
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("%test\\%", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle LIKE @p1 ESCAPE '\'
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringEquals()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Test", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "test", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "Other", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where book.Title.Equals("Test")
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("Test", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Single(results);
        Assert.Equal("Test", results[0].Title);
    }

    [Fact]
    public void StringIndexOf()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "atestb", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "test", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "book", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where book.Title.IndexOf("test") == 1
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("test", command.Parameters[0].Value);
        Assert.Equal(1, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE INSTR(b0.BookTitle, @p0) - 1 = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Single(results);
        Assert.Equal("atestb", results[0].Title);
    }

    [Fact]
    public void StringReplace()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "ae", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "test", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "banana", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where book.Title.Replace("a", "b") == "be"
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal("a", command.Parameters[0].Value);
        Assert.Equal("b", command.Parameters[1].Value);
        Assert.Equal("be", command.Parameters[2].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE REPLACE(b0.BookTitle, @p0, @p1) = @p2
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Single(results);
        Assert.Equal("ae", results[0].Title);
    }

    [Fact]
    public void StringTrim()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "  be  ", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "test", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = " be ", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where book.Title.Trim() == "be"
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("be", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE TRIM(b0.BookTitle) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Title == "  be  ");
        Assert.Contains(results, b => b.Title == " be ");
    }

    [Fact]
    public void StringTrimOne()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = " be ", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "test", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "  be  ", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where book.Title.Trim(' ') == "be"
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(' ', command.Parameters[0].Value);
        Assert.Equal("be", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE TRIM(b0.BookTitle, @p0) = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Title == " be ");
        Assert.Contains(results, b => b.Title == "  be  ");
    }

    [Fact]
    public void StringTrimMultiple()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = " abc be cba ", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "test", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "cbabecba", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where book.Title.Trim(' ', 'a', 'b', 'c') == "e"
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(5, command.Parameters.Count);
        Assert.Equal(' ', command.Parameters[0].Value);
        Assert.Equal('a', command.Parameters[1].Value);
        Assert.Equal('b', command.Parameters[2].Value);
        Assert.Equal('c', command.Parameters[3].Value);
        Assert.Equal("e", command.Parameters[4].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE TRIM(b0.BookTitle, @p1 || @p2 || @p3 || @p4) = @p5
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Title == " abc be cba ");
        Assert.Contains(results, b => b.Title == "cbabecba");
    }

    [Fact]
    public void StringTrimStart()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "  be", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "test", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "   be", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where book.Title.TrimStart() == "be"
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("be", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE LTRIM(b0.BookTitle) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Title == "  be");
        Assert.Contains(results, b => b.Title == "   be");
    }

    [Fact]
    public void StringTrimEnd()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "be  ", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "test", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "be   ", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where book.Title.TrimEnd() == "be"
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("be", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE RTRIM(b0.BookTitle) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Title == "be  ");
        Assert.Contains(results, b => b.Title == "be   ");
    }

    [Fact]
    public void StringSubstring()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "abe", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "test", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "xbe", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where book.Title.Substring(1) == "be"
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("be", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE SUBSTR(b0.BookTitle, @p0 + 1) = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Title == "abe");
        Assert.Contains(results, b => b.Title == "xbe");
    }

    [Fact]
    public void StringSubstringRange()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "abef", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "test", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "xbey", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query =
            from book in db.Table<Book>()
            where book.Title.Substring(1, 2) == "be"
            select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal("be", command.Parameters[2].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE SUBSTR(b0.BookTitle, @p0 + 1, @p1) = @p2
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Title == "abef");
        Assert.Contains(results, b => b.Title == "xbey");
    }

    [Fact]
    public void StringEqualsUpper()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "be", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "BE", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "test", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query =
            from book in db.Table<Book>()
            where string.Equals(book.Title, "BE", StringComparison.OrdinalIgnoreCase)
            select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("BE", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookTitle = @p0 COLLATE NOCASE)
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Title == "be");
        Assert.Contains(results, b => b.Title == "BE");
    }

    [Fact]
    public void StringEqualsLower()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "BE", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "be", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "TEST", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query =
            from book in db.Table<Book>()
            where string.Equals(book.Title, "be", StringComparison.OrdinalIgnoreCase)
            select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("be", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookTitle = @p0 COLLATE NOCASE)
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Title == "BE");
        Assert.Contains(results, b => b.Title == "be");
    }

    [Fact]
    public void StringEqualsNoStringComparison()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "BE", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "be", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "TEST", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query =
            from book in db.Table<Book>()
            where string.Equals(book.Title, "be")
            select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("be", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookTitle = @p0)
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Single(results);
        Assert.Contains(results, b => b.Title == "be");
    }

    [Fact]
    public void MathMin()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book1", AuthorId = 2, Price = 10 },
            new Book { Id = 2, Title = "Book2", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "Book3", AuthorId = 3, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where Math.Min(book.Id, book.AuthorId) == 1
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (CASE WHEN b0.BookId < b0.BookAuthorId THEN b0.BookId ELSE b0.BookAuthorId END) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Id == 1);
        Assert.Contains(results, b => b.Id == 2);
    }

    [Fact]
    public void MathMax()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book2", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "Book3", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where Math.Max(book.Id, book.AuthorId) == 1
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (CASE WHEN b0.BookId > b0.BookAuthorId THEN b0.BookId ELSE b0.BookAuthorId END) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void MathAbs()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book1", AuthorId = 1, Price = 10 },
            new Book { Id = -1, Title = "Book2", AuthorId = 1, Price = 15 },
            new Book { Id = 2, Title = "Book3", AuthorId = 2, Price = 20 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where Math.Abs(book.Id) == 1
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE ABS(b0.BookId) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Id == 1);
        Assert.Contains(results, b => b.Id == -1);
    }

    [Fact]
    public void MathRound()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Round((double)book.Id) == 1
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE ROUND(CAST(b0.BookId AS REAL)) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathCeiling()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book1", AuthorId = 1, Price = 10.1 },
            new Book { Id = 2, Title = "Book2", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "Book3", AuthorId = 2, Price = 20.9 }
        });

        IQueryable<Book> query = from book in db.Table<Book>()
                                 where Math.Ceiling((double)book.Id) == 1
                                 select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE CEIL(CAST(b0.BookId AS REAL)) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void MathFloor()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Floor((double)book.Id) == 1
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1d, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE FLOOR(CAST(b0.BookId AS REAL)) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void StringLastIndexOf()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "test-test", AuthorId = 1, Price = 10.01 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "hello", AuthorId = 1, Price = 10.02 });

        var query = db.Table<Book>().Select(b => new { b.Id, LastIndex = b.Title.LastIndexOf("test") });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("test", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            CASE WHEN INSTR(b0.BookTitle, @p0) = 0 THEN -1 ELSE LENGTH(b0.BookTitle) - INSTR(REPLACE(REPLACE(b0.BookTitle, REPLACE(@p0, '%', '\%'), '<<<>>>'), '<<<>>>', ''), '<<<>>>') - LENGTH(REPLACE(@p0, '%', '\%')) END AS "LastIndex"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal(5, results[0].LastIndex);
        Assert.Equal(-1, results[1].LastIndex);
    }

    [Fact]
    public void StringInsert()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Test", AuthorId = 1, Price = 11.01 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "Book", AuthorId = 1, Price = 11.02 });

        var query = db.Table<Book>().Select(b => new { b.Id, Inserted = b.Title.Insert(2, "XX") });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(2, command.Parameters[0].Value);
        Assert.Equal("XX", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            SUBSTR(b0.BookTitle, 1, @p0) || @p1 || SUBSTR(b0.BookTitle, @p0 + 1) AS "Inserted"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("TeXXst", results[0].Inserted);
        Assert.Equal("BoXXok", results[1].Inserted);
    }

    [Fact]
    public void StringRemoveOneArg()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Testing", AuthorId = 1, Price = 12.01 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "Book", AuthorId = 1, Price = 12.02 });

        var query = db.Table<Book>().Select(b => new { b.Id, Removed = b.Title.Remove(4) });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(4, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            SUBSTR(b0.BookTitle, 1, @p0) AS "Removed"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("Test", results[0].Removed);
        Assert.Equal("Book", results[1].Removed);
    }

    [Fact]
    public void StringRemoveTwoArgs()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Testing", AuthorId = 1, Price = 13.01 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "Book", AuthorId = 1, Price = 13.02 });

        var query = db.Table<Book>().Select(b => new { b.Id, Removed = b.Title.Remove(1, 3) });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(3, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            SUBSTR(b0.BookTitle, 1, @p0) || SUBSTR(b0.BookTitle, @p0 + @p1 + 1) AS "Removed"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("Ting", results[0].Removed);
        Assert.Equal("B", results[1].Removed);
    }

    [Fact]
    public void StringConcat()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Test", AuthorId = 1, Price = 14.01 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "Sample", AuthorId = 1, Price = 14.02 });

        var query = db.Table<Book>().Select(b => new { b.Id, Concatenated = string.Concat(b.Title, " - ", "Book") });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(" - ", command.Parameters[0].Value);
        Assert.Equal("Book", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle || @p0 || @p1 AS "Concatenated"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("Test - Book", results[0].Concatenated);
        Assert.Equal("Sample - Book", results[1].Concatenated);
    }

    [Fact]
    public void StringCompare()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 15.01 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "Test", AuthorId = 1, Price = 15.02 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "Zebra", AuthorId = 1, Price = 15.03 });

        var query = db.Table<Book>().Select(b => new { b.Id, Comparison = string.Compare(b.Title, "Test") });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("Test", command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            (CASE WHEN b0.BookTitle = @p0 THEN 0 WHEN b0.BookTitle < @p0 THEN -1 ELSE 1 END) AS "Comparison"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(3, results.Count);
        Assert.Equal(-1, results[0].Comparison);
        Assert.Equal(0, results[1].Comparison);
        Assert.Equal(1, results[2].Comparison);
    }

    [Fact]
    public void StringPadLeftWithChar()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Test", AuthorId = 1, Price = 16.01 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "VeryLongTitle", AuthorId = 1, Price = 16.02 });

        var query = db.Table<Book>().Select(b => new { b.Id, Padded = b.Title.PadLeft(8, 'x') });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(8, command.Parameters[0].Value);
        Assert.Equal('x', command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            (CASE WHEN LENGTH(b0.BookTitle) >= @p0 THEN b0.BookTitle ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(@p0 - LENGTH(b0.BookTitle))), '00', @p1), 1, @p0 - LENGTH(b0.BookTitle)) || b0.BookTitle) END) AS "Padded"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("xxxxTest", results[0].Padded);
        Assert.Equal("VeryLongTitle", results[1].Padded);
    }

    [Fact]
    public void StringPadRightWithChar()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Test", AuthorId = 1, Price = 17.01 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "VeryLongTitle", AuthorId = 1, Price = 17.02 });

        var query = db.Table<Book>().Select(b => new { b.Id, Padded = b.Title.PadRight(8, 'x') });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(8, command.Parameters[0].Value);
        Assert.Equal('x', command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            (CASE WHEN LENGTH(b0.BookTitle) >= @p0 THEN b0.BookTitle ELSE (b0.BookTitle || (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(@p0 - LENGTH(b0.BookTitle))), '00', @p1), 1, @p0 - LENGTH(b0.BookTitle)))) END) AS "Padded"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("Testxxxx", results[0].Padded);
        Assert.Equal("VeryLongTitle", results[1].Padded);
    }

    [Fact]
    public void StringToUpperInvariant()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Test", AuthorId = 1, Price = 18.01 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "book", AuthorId = 1, Price = 18.02 });

        var query = db.Table<Book>().Select(b => new { b.Id, Upper = b.Title.ToUpperInvariant() });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            UPPER(b0.BookTitle) AS "Upper"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("TEST", results[0].Upper);
        Assert.Equal("BOOK", results[1].Upper);
    }

    [Fact]
    public void StringToLowerInvariant()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "TEST", AuthorId = 1, Price = 19.01 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "Book", AuthorId = 1, Price = 19.02 });

        var query = db.Table<Book>().Select(b => new { b.Id, Lower = b.Title.ToLowerInvariant() });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            LOWER(b0.BookTitle) AS "Lower"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("test", results[0].Lower);
        Assert.Equal("book", results[1].Lower);
    }

    [Fact]
    public void EmptyListContains()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Test", AuthorId = 1, Price = 20.01 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "Book", AuthorId = 1, Price = 20.02 });

        List<int> emptyList = [];

        IQueryable<Book> query = db.Table<Book>().Where(book => emptyList.Contains(book.Id));

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE 0 = 1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Empty(results);
    }

    [Fact]
    public void StringJoin()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Author>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book1", AuthorId = 1, Price = 10 }
        });
        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "John",
            Email = "john@test.com",
            BirthDate = new DateTime(2000, 1, 1)
        });

        IQueryable<Book> query =
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            where string.Join(" - ", new[] { book.Title, author.Name }) == "Book1 - John"
            select book;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(" - ", command.Parameters[0].Value);
        Assert.Equal("Book1 - John", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     JOIN "Authors" AS a1 ON b0.BookAuthorId = a1.AuthorId
                     WHERE b0.BookTitle || @p0 || a1.AuthorName = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<Book> results = query.ToList();
        Assert.Single(results);
        Assert.Equal("Book1", results[0].Title);
    }
}
