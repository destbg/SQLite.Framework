using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupByTests
{
    [Fact]
    public void GroupByTableWithMultiResult()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            where g.Count() > 1
            select new { Count = g.Count(), Id = g.Key }
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT COUNT(*) AS "Count",
                            b0.BookAuthorId AS "Id"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     HAVING COUNT(*) > @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupBySumTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Sum(f => f.Price)
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT SUM(b0.BookPrice) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupBySimpleSumTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book.Price by book.AuthorId
            into g
            select g.Sum()
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT SUM(b0.BookPrice) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByComplexSumTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group new { book.Price, book.AuthorId } by book.AuthorId
            into g
            select g.Sum(f => f.Price)
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT SUM(b0.BookPrice) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByAverageTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Average(f => f.Price)
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT AVG(b0.BookPrice) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByMinTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Min(f => f.Price)
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT MIN(b0.BookPrice) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByMaxTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Max(f => f.Price)
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT MAX(b0.BookPrice) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByCountTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Count()
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT COUNT(*) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByLongCountTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.LongCount()
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT COUNT(*) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByCountHavingTable()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            where g.Count() > 1
            select g.Count()
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT COUNT(*) AS "8"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     HAVING COUNT(*) > @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task GroupBy_ClientSideAfterToListAsync_Works()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        });

        List<Book> rows = await db.Table<Book>().ToListAsync(TestContext.Current.CancellationToken);
        Dictionary<int, List<Book>> byAuthor = rows
            .GroupBy(b => b.AuthorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        Assert.Equal(2, byAuthor.Count);
        Assert.Equal(2, byAuthor[1].Count);
        Assert.Single(byAuthor[2]);
    }

    [Fact]
    public void GroupBy_ToList_YieldsGroupingsInInsertionOrder()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 7, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 3, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 7, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 3, Price = 4 },
        });

        List<IGrouping<int, Book>> groups = db.Table<Book>()
            .OrderBy(b => b.Id)
            .GroupBy(b => b.AuthorId)
            .ToList();

        Assert.Equal(2, groups.Count);
        Assert.Equal(7, groups[0].Key);
        Assert.Equal(3, groups[1].Key);
        Assert.Equal(new[] { "A", "C" }, groups[0].Select(b => b.Title));
        Assert.Equal(new[] { "B", "D" }, groups[1].Select(b => b.Title));
    }

    [Fact]
    public void GroupBy_ToDictionary_BuildsDictionaryOfLists()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        });

        Dictionary<int, List<Book>> byAuthor = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        Assert.Equal(2, byAuthor.Count);
        Assert.Equal(new[] { "A", "B" }, byAuthor[1].Select(b => b.Title));
        Assert.Equal(new[] { "C" }, byAuthor[2].Select(b => b.Title));
    }

    [Fact]
    public void GroupBy_ForEach_EnumeratesElementsPerGroup()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 10, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 10, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 20, Price = 3 },
        });

        int totalRows = 0;
        int totalGroups = 0;
        foreach (IGrouping<int, Book> group in db.Table<Book>().GroupBy(b => b.AuthorId))
        {
            totalGroups++;
            foreach (Book _ in group)
            {
                totalRows++;
            }
        }

        Assert.Equal(2, totalGroups);
        Assert.Equal(3, totalRows);
    }

    [Fact]
    public void GroupBy_CompositeKey_AnonymousType()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1.0 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2.0 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 5.0 },
            new Book { Id = 4, Title = "D", AuthorId = 2, Price = 4.0 },
        });

        Dictionary<(int AuthorId, bool Cheap), List<string>> byKey = db.Table<Book>()
            .OrderBy(b => b.Id)
            .GroupBy(b => new { b.AuthorId, Cheap = b.Price < 3.0 })
            .ToDictionary(g => (g.Key.AuthorId, g.Key.Cheap), g => g.Select(b => b.Title).ToList());

        Assert.Equal(3, byKey.Count);
        Assert.Equal(new[] { "A", "B" }, byKey[(1, true)]);
        Assert.Equal(new[] { "C" }, byKey[(1, false)]);
        Assert.Equal(new[] { "D" }, byKey[(2, false)]);
    }

    [Fact]
    public void GroupBy_WithWhereAndOrderBy_PreservesFilter()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 50 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 100 },
        });

        List<IGrouping<int, Book>> groups = db.Table<Book>()
            .Where(b => b.Price > 10)
            .OrderBy(b => b.Id)
            .GroupBy(b => b.AuthorId)
            .ToList();

        Assert.Equal(2, groups.Count);
        Assert.Equal(new[] { 1, 2 }, groups.Select(g => g.Key));
        Assert.Single(groups[0]);
        Assert.Equal("B", groups[0].Single().Title);
        Assert.Single(groups[1]);
        Assert.Equal("C", groups[1].Single().Title);
    }

    [Fact]
    public void GroupBy_EmptyResult_ReturnsEmpty()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();

        List<IGrouping<int, Book>> groups = db.Table<Book>().GroupBy(b => b.AuthorId).ToList();

        Assert.Empty(groups);
    }
}