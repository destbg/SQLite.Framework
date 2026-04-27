using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WhereTests
{
    [Fact]
    public void EqualWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id == 1
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void NotEqualWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id != 1
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId <> @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void NotNotEqualWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where !(book.Id != 1)
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE NOT b0.BookId <> @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GreaterThanWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id > 1
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId > @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GreaterThanOrEqualWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id >= 1
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId >= @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void LessThanWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id < 1
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId < @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void LessThanOrEqualWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id <= 1
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId <= @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void AddWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id + 1 > 2
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookId + @p0) > @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void SubtractWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id - 1 > 2
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookId - @p0) > @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MultiplyWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id * 1 > 2
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookId * @p0) > @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void DivideWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id / 1 > 2
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE (b0.BookId / @p0) > @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void AndWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id == 1 && book.AuthorId == 2
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId = @p0 AND b0.BookAuthorId = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void OrWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id == 1 || book.AuthorId == 2
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId = @p0 OR b0.BookAuthorId = @p1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void IsWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title == null
            select book
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle IS NULL
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void IsNotWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Title != null
            select book
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookTitle IS NOT NULL
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ConditionalWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where (book.Title != null ? 1 : 2) == 1
            select book
        ).ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(2, command.Parameters[1].Value);
        Assert.Equal(1, command.Parameters[2].Value);
        Assert.Equal($"""
                      SELECT b0.BookId AS "Id",
                             b0.BookTitle AS "Title",
                             b0.BookAuthorId AS "AuthorId",
                             b0.BookPrice AS "Price"
                      FROM "Books" AS b0
                      WHERE (CASE WHEN b0.BookTitle IS NOT NULL THEN @p1 ELSE @p2 END) = @p3
                      """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void CoalesceWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where (book.Title ?? "") == "Book"
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("", command.Parameters[0].Value);
        Assert.Equal("Book", command.Parameters[1].Value);
        Assert.Equal($"""
                      SELECT b0.BookId AS "Id",
                             b0.BookTitle AS "Title",
                             b0.BookAuthorId AS "AuthorId",
                             b0.BookPrice AS "Price"
                      FROM "Books" AS b0
                      WHERE COALESCE(b0.BookTitle, @p0) = @p1
                      """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ComplexWhere()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id == 1
                  && !(book.Id != 3)
                  && (book.Id == 18 || book.AuthorId == 19)
                  && book.Title == null
                  && book.Title != "Test"
                  && (book.Title != null ? 20 : 21) == 22
                  && (book.Title ?? "") == "Book"
            select book
        ).ToSqlCommand();

        Assert.Equal(10, command.Parameters.Count);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(3, command.Parameters[1].Value);
        Assert.Equal(18, command.Parameters[2].Value);
        Assert.Equal(19, command.Parameters[3].Value);
        Assert.Equal("Test", command.Parameters[4].Value);
        Assert.Equal(20, command.Parameters[5].Value);
        Assert.Equal(21, command.Parameters[6].Value);
        Assert.Equal(22, command.Parameters[7].Value);
        Assert.Equal("", command.Parameters[8].Value);
        Assert.Equal("Book", command.Parameters[9].Value);
        Assert.Equal("""
                      SELECT b0.BookId AS "Id",
                             b0.BookTitle AS "Title",
                             b0.BookAuthorId AS "AuthorId",
                             b0.BookPrice AS "Price"
                      FROM "Books" AS b0
                      WHERE b0.BookId = @p0 AND NOT b0.BookId <> @p1 AND b0.BookId = @p2 OR b0.BookAuthorId = @p3 AND b0.BookTitle IS NULL AND b0.BookTitle <> @p5 AND (CASE WHEN b0.BookTitle IS NOT NULL THEN @p7 ELSE @p8 END) = @p9 AND COALESCE(b0.BookTitle, @p10) = @p11
                      """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Where_Between_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 1, Price = 3 });
        db.Table<Book>().Add(new Book { Id = 4, Title = "d", AuthorId = 1, Price = 4 });
        db.Table<Book>().Add(new Book { Id = 5, Title = "e", AuthorId = 1, Price = 5 });

        List<int> ids = db.Table<Book>()
            .Where(b => SQLiteFunctions.Between(b.Id, 2, 4))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([2, 3, 4], ids);
    }

    [Fact]
    public void Where_Between_EmitsBetweenSql()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Where(b => SQLiteFunctions.Between(b.Id, 2, 4))
            .ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(2, command.Parameters[0].Value);
        Assert.Equal(4, command.Parameters[1].Value);
        Assert.Contains("BETWEEN @p0 AND @p1", command.CommandText);
    }

    [Fact]
    public void Where_NotBetween_ViaNegation_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 1, Price = 3 });
        db.Table<Book>().Add(new Book { Id = 4, Title = "d", AuthorId = 1, Price = 4 });
        db.Table<Book>().Add(new Book { Id = 5, Title = "e", AuthorId = 1, Price = 5 });

        List<int> ids = db.Table<Book>()
            .Where(b => !SQLiteFunctions.Between(b.Id, 2, 4))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([1, 5], ids);
    }

    [Fact]
    public void Where_NotBetween_ViaEqualityFalse_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 1, Price = 3 });

        List<int> ids = db.Table<Book>()
            .Where(b => SQLiteFunctions.Between(b.Id, 2, 3) == false)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Where_Between_WithCapturedBounds_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 7, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 8, Title = "b", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 9, Title = "c", AuthorId = 1, Price = 3 });

        int low = 7;
        int high = 8;
        List<int> ids = db.Table<Book>()
            .Where(b => SQLiteFunctions.Between(b.Id, low, high))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([7, 8], ids);
    }

    [Fact]
    public void SQLiteFunctions_Between_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Between(1, 0, 2));
    }

    [Fact]
    public void Where_In_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 1, Price = 3 });

        List<int> ids = db.Table<Book>()
            .Where(b => SQLiteFunctions.In(b.Id, 1, 3))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Where_In_EmitsInSql()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Where(b => SQLiteFunctions.In(b.Id, 1, 3))
            .ToSqlCommand();

        Assert.Contains(" IN (", command.CommandText);
        Assert.Equal(2, command.Parameters.Count);
    }

    [Fact]
    public void Where_NotIn_ViaNegation_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 1, Price = 3 });

        List<int> ids = db.Table<Book>()
            .Where(b => !SQLiteFunctions.In(b.Id, 1, 3))
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void SQLiteFunctions_In_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.In(1, 1, 2));
    }

    [Fact]
    public void Where_In_WithCapturedArray_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 1, Price = 3 });

        int[] wanted = [1, 3];
        List<int> ids = db.Table<Book>()
            .Where(b => SQLiteFunctions.In(b.Id, wanted))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Where_In_WithCapturedArray_EmitsExpandedParams()
    {
        using TestDatabase db = new();

        int[] wanted = [1, 3];
        SQLiteCommand command = db.Table<Book>()
            .Where(b => SQLiteFunctions.In(b.Id, wanted))
            .ToSqlCommand();

        Assert.Contains(" IN (", command.CommandText);
        Assert.Equal(2, command.Parameters.Count);
    }

    [Fact]
    public void Where_Coalesce_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });

        List<int> ids = db.Table<Book>()
            .Where(b => SQLiteFunctions.Coalesce<string>(null, b.Title) == "b")
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Where_Coalesce_EmitsCoalesceSql()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Where(b => SQLiteFunctions.Coalesce<string>(null, b.Title) == "b")
            .ToSqlCommand();

        Assert.Contains("coalesce(", command.CommandText);
    }

    [Fact]
    public void SQLiteFunctions_Coalesce_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Coalesce<string>(null, "x"));
    }

    [Fact]
    public void Where_Nullif_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 1, Price = 3 });

        List<int> ids = db.Table<Book>()
            .Where(b => SQLiteFunctions.Nullif(b.Title, "b") == null)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Where_Nullif_EmitsNullifSql()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Where(b => SQLiteFunctions.Nullif(b.Title, "b") == null)
            .ToSqlCommand();

        Assert.Contains("nullif(", command.CommandText);
    }

    [Fact]
    public void SQLiteFunctions_Nullif_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Nullif(1, 1));
    }

    [Fact]
    public void Select_Typeof_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1.5 });

        List<string> types = db.Table<Book>()
            .Select(b => SQLiteFunctions.Typeof(b.Price))
            .ToList();

        Assert.Equal(["real"], types);
    }

    [Fact]
    public void SQLiteFunctions_Typeof_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Typeof(1));
    }

    [Fact]
    public void Select_Hex_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        byte[] data = [0xDE, 0xAD];
        string hex = db.Table<Book>()
            .Select(_ => SQLiteFunctions.Hex(data))
            .First();

        Assert.Equal("DEAD", hex);
    }

    [Fact]
    public void SQLiteFunctions_Hex_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Hex([1, 2]));
    }

    [Fact]
    public void Select_Quote_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        string quoted = db.Table<Book>()
            .Select(b => SQLiteFunctions.Quote(b.Title))
            .First();

        Assert.Equal("'a'", quoted);
    }

    [Fact]
    public void SQLiteFunctions_Quote_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Quote(1));
    }

    [Fact]
    public void Select_Zeroblob_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        byte[] zeros = db.Table<Book>()
            .Select(_ => SQLiteFunctions.Zeroblob(4))
            .First();

        Assert.Equal(new byte[] { 0, 0, 0, 0 }, zeros);
    }

    [Fact]
    public void SQLiteFunctions_Zeroblob_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Zeroblob(2));
    }

    [Fact]
    public void Where_Instr_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "alpha", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "alphabet", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "beta", AuthorId = 1, Price = 3 });

        List<int> ids = db.Table<Book>()
            .Where(b => SQLiteFunctions.Instr(b.Title, "lph") > 0)
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([1, 2], ids);
    }

    [Fact]
    public void SQLiteFunctions_Instr_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Instr("abc", "b"));
    }

    [Fact]
    public void Select_LastInsertRowId_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 7, Title = "a", AuthorId = 1, Price = 1 });

        long rowId = db.Table<Book>()
            .Select(_ => SQLiteFunctions.LastInsertRowId())
            .First();

        Assert.Equal(7, rowId);
    }

    [Fact]
    public void SQLiteFunctions_LastInsertRowId_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.LastInsertRowId());
    }

    [Fact]
    public void Select_SqliteVersion_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        string version = db.Table<Book>()
            .Select(_ => SQLiteFunctions.SqliteVersion())
            .First();

        Assert.False(string.IsNullOrEmpty(version));
        Assert.Contains(".", version);
    }

    [Fact]
    public void SQLiteFunctions_SqliteVersion_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.SqliteVersion());
    }

    [Fact]
    public void Where_ScalarMin_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 9, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 9, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 9, Price = 3 });

        List<int> ids = db.Table<Book>()
            .Where(b => SQLiteFunctions.Min(b.Id, b.AuthorId) == 2)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Where_ScalarMin_EmitsScalarMinSql()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Where(b => SQLiteFunctions.Min(b.Id, b.AuthorId) == 2)
            .ToSqlCommand();

        Assert.Contains("min(", command.CommandText);
        Assert.Contains(",", command.CommandText[command.CommandText.IndexOf("min(", StringComparison.Ordinal)..]);
    }

    [Fact]
    public void Where_ScalarMax_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 0, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 0, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 0, Price = 3 });

        List<int> ids = db.Table<Book>()
            .Where(b => SQLiteFunctions.Max(b.Id, b.AuthorId) == 2)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Where_ScalarMax_EmitsScalarMaxSql()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Where(b => SQLiteFunctions.Max(b.Id, b.AuthorId) == 2)
            .ToSqlCommand();

        Assert.Contains("max(", command.CommandText);
        Assert.Contains(",", command.CommandText[command.CommandText.IndexOf("max(", StringComparison.Ordinal)..]);
    }

    [Fact]
    public void SQLiteFunctions_Min_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Min(1, 2));
    }

    [Fact]
    public void SQLiteFunctions_Max_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Max(1, 2));
    }
}