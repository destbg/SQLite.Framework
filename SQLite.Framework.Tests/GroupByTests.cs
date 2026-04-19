using System.Runtime.CompilerServices;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupByTests
{
    private static void SkipIfAot()
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            Assert.Skip("Materializing IGrouping<TKey, TElement> with a value-type key requires MakeGenericMethod, which is not supported under Native AOT.");
        }
    }

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
    public async Task GroupBy_ToDictionaryAsync_WithToListValueSelector()
    {
        SkipIfAot();
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        });

        Dictionary<int, List<Book>> byAuthor = await db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList());

        Assert.Equal(2, byAuthor.Count);
        Assert.Equal(2, byAuthor[1].Count);
        Assert.Single(byAuthor[2]);
    }

    [Fact]
    public async Task GroupBy_ClientSideAfterToListAsync_AotCompatible()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        });

        List<Book> rows = await db.Table<Book>().ToListAsync();
        Dictionary<int, List<Book>> byAuthor = rows
            .GroupBy(b => b.AuthorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        Assert.Equal(2, byAuthor.Count);
        Assert.Equal(2, byAuthor[1].Count);
        Assert.Single(byAuthor[2]);
    }

    [Fact]
    public void GroupBy_ToList_ReturnsGroupings()
    {
        SkipIfAot();
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 2, Price = 4 },
            new Book { Id = 5, Title = "E", AuthorId = 3, Price = 5 },
        });

        List<IGrouping<int, Book>> groups = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .ToList();

        Assert.Equal(3, groups.Count);

        IGrouping<int, Book> g1 = groups.Single(g => g.Key == 1);
        Assert.Equal(2, g1.Count());
        Assert.All(g1, b => Assert.Equal(1, b.AuthorId));

        IGrouping<int, Book> g2 = groups.Single(g => g.Key == 2);
        Assert.Equal(2, g2.Count());

        IGrouping<int, Book> g3 = groups.Single(g => g.Key == 3);
        Assert.Single(g3);
    }

    [Fact]
    public async Task GroupBy_ToListAsync_ReturnsGroupings()
    {
        SkipIfAot();
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        });

        List<IGrouping<int, Book>> groups = await db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .ToListAsync();

        Assert.Equal(2, groups.Count);
        Assert.Equal(2, groups.Single(g => g.Key == 1).Count());
        Assert.Single(groups.Single(g => g.Key == 2));
    }

    [Fact]
    public async Task GroupBy_ToArrayAsync_ReturnsGroupings()
    {
        SkipIfAot();
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
        });

        IGrouping<int, Book>[] groups = await db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .ToArrayAsync();

        Assert.Equal(2, groups.Length);
    }

    [Fact]
    public void GroupBy_Foreach_IteratesEachGrouping()
    {
        SkipIfAot();
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        });

        Dictionary<int, int> counts = new();
        foreach (IGrouping<int, Book> g in db.Table<Book>().GroupBy(b => b.AuthorId))
        {
            counts[g.Key] = g.Count();
        }

        Assert.Equal(2, counts.Count);
        Assert.Equal(2, counts[1]);
        Assert.Equal(1, counts[2]);
    }

    [Fact]
    public async Task GroupBy_WithWhere_GroupsFilteredRows()
    {
        SkipIfAot();
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 5 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 20 },
            new Book { Id = 4, Title = "D", AuthorId = 3, Price = 1 },
        });

        List<IGrouping<int, Book>> groups = await db.Table<Book>()
            .Where(b => b.Price >= 5)
            .GroupBy(b => b.AuthorId)
            .ToListAsync();

        Assert.Equal(2, groups.Count);
        Assert.DoesNotContain(groups, g => g.Key == 3);
        Assert.Equal(2, groups.Single(g => g.Key == 1).Count());
    }

    [Fact]
    public void GroupBy_KeyIsIdentity_GroupsByWholeRow()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
        });

        List<IGrouping<Book, Book>> groups = db.Table<Book>()
            .GroupBy(b => b)
            .ToList();

        Assert.Equal(2, groups.Count);
        Assert.All(groups, g => Assert.Single(g));
    }

    [Fact]
    public void GroupBy_NestedMemberKey_GroupsCorrectly()
    {
        SkipIfAot();
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Foo", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Foo", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "Bar", AuthorId = 2, Price = 3 },
        });

        Dictionary<int, List<Book>> byTitleLength = db.Table<Book>()
            .GroupBy(b => b.Title.Length)
            .ToDictionary(g => g.Key, g => g.ToList());

        Assert.Single(byTitleLength);
        Assert.Equal(3, byTitleLength.Keys.Single());
        Assert.Equal(3, byTitleLength[3].Count);
    }

    [Fact]
    public async Task GroupBy_EmptySource_YieldsNoGroups()
    {
        SkipIfAot();
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        List<IGrouping<int, Book>> groups = await db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .ToListAsync();

        Assert.Empty(groups);
    }

    [Fact]
    public void GroupBy_Count_ReturnsNumberOfGroups()
    {
        SkipIfAot();
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 3, Price = 4 },
        });

        int count = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .ToList()
            .Count;

        Assert.Equal(3, count);
    }
}