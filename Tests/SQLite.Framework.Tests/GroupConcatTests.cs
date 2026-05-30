using System.Reflection;
#if !SQLITE_FRAMEWORK_BUNDLED && !SQLITECIPHER && !NO_SQLITEPCL_RAW_BATTERIES
using SQLite.Framework.Enums;
#endif
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupConcatTests
{
    [Fact]
    public void StringJoin_SubqueryWithCustomSeparator_EmitsGroupConcat()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Author>()
            .Select(a => string.Join(", ", db.Table<Book>().Where(b => b.AuthorId == a.Id).Select(b => b.Title)))
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT (
                         SELECT group_concat(b2."BookTitle", @p1) AS "19"
                         FROM "Books" AS b2
                         WHERE b2."BookAuthorId" = a0."AuthorId"
                     ) AS "21"
                     FROM "Authors" AS a0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
        Assert.Single(command.Parameters);
        Assert.Equal(", ", command.Parameters[0].Value);
    }

    [Fact]
    public void StringJoin_SubqueryReturnsConcatenatedRows()
    {
        using TestDatabase db = new();

        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().AddRange(new[]
        {
            new Author { Id = 1, Name = "Anna", Email = "a@example.com", BirthDate = new DateTime(1980, 1, 1) },
            new Author { Id = 2, Name = "Ben", Email = "b@example.com", BirthDate = new DateTime(1981, 1, 1) },
        });
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Alpha", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Beta", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "Gamma", AuthorId = 2, Price = 3 },
        });

        List<string> rows = db.Table<Author>()
            .OrderBy(a => a.Id)
            .Select(a => string.Join(", ", db.Table<Book>()
                .Where(b => b.AuthorId == a.Id)
                .OrderBy(b => b.Id)
                .Select(b => b.Title)))
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("Alpha, Beta", rows[0]);
        Assert.Equal("Gamma", rows[1]);
    }

    [Fact]
    public void StringJoin_SubqueryWithNoRows_ReturnsNull()
    {
        using TestDatabase db = new();

        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "Anna", Email = "a@example.com", BirthDate = new DateTime(1980, 1, 1) });

        string? row = db.Table<Author>()
            .Select(a => string.Join(", ", db.Table<Book>()
                .Where(b => b.AuthorId == a.Id)
                .Select(b => b.Title)))
            .First();

        Assert.Null(row);
    }

    [Fact]
    public void StringJoin_SubqueryWithDistinct_Throws()
    {
        using TestDatabase db = new();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Author>()
                .Select(a => string.Join("|", db.Table<Book>()
                    .Where(b => b.AuthorId == a.Id)
                    .Select(b => b.Title)
                    .Distinct()))
                .ToSqlCommand());

        Assert.Contains("group_concat", ex.Message);
        Assert.Contains("DISTINCT", ex.Message);
    }

    [Fact]
    public void StringJoin_SubqueryOverIntColumn_CastsToText()
    {
        using TestDatabase db = new();

        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "Anna", Email = "a@example.com", BirthDate = new DateTime(1980, 1, 1) });
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 10, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 20, Title = "B", AuthorId = 1, Price = 2 },
        });

        string row = db.Table<Author>()
            .Select(a => string.Join("-", db.Table<Book>()
                .Where(b => b.AuthorId == a.Id)
                .OrderBy(b => b.Id)
                .Select(b => b.Id)))
            .First()!;

        Assert.Equal("10-20", row);
    }

    [Fact]
    public void StringJoin_SubqueryWithMultipleColumns_Throws()
    {
        using TestDatabase db = new();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Author>()
                .Select(a => string.Join(", ", db.Table<Book>()
                    .Where(b => b.AuthorId == a.Id)))
                .ToSqlCommand());
    }

    [Fact]
    public void StringJoin_SubqueryAfterTake_Throws()
    {
        using TestDatabase db = new();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Author>()
                .Select(a => string.Join(", ", db.Table<Book>()
                    .Where(b => b.AuthorId == a.Id)
                    .Select(b => b.Title)
                    .Take(5)))
                .ToSqlCommand());
    }

    [Fact]
    public void StringJoin_SubqueryAfterSkip_Throws()
    {
        using TestDatabase db = new();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Author>()
                .Select(a => string.Join(", ", db.Table<Book>()
                    .Where(b => b.AuthorId == a.Id)
                    .Select(b => b.Title)
                    .Skip(2)))
                .ToSqlCommand());
    }

#if !SQLITECIPHER
    [Fact]
    public void StringJoin_SubqueryWithOrderBy_EmitsOrderByInsideGroupConcat()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Author>()
            .Select(a => string.Join(", ", db.Table<Book>()
                .Where(b => b.AuthorId == a.Id)
                .Select(b => b.Title)
                .OrderBy(t => t)))
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT (
                         SELECT group_concat(b2."BookTitle", @p1 ORDER BY b2."BookTitle" ASC) AS "21"
                         FROM "Books" AS b2
                         WHERE b2."BookAuthorId" = a0."AuthorId"
                     ) AS "23"
                     FROM "Authors" AS a0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
        Assert.Single(command.Parameters);
        Assert.Equal(", ", command.Parameters[0].Value);
    }

    [Fact]
    public void StringJoin_SubqueryWithOrderByDescendingThenBy_EmitsMultipleSortKeys()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Author>()
            .Select(a => string.Join(", ", db.Table<Book>()
                .Where(b => b.AuthorId == a.Id)
                .OrderByDescending(b => b.Price)
                .ThenBy(b => b.Id)
                .Select(b => b.Title)))
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT (
                         SELECT group_concat(b2."BookTitle", @p1 ORDER BY b2."BookPrice" DESC, b2."BookId" ASC) AS "23"
                         FROM "Books" AS b2
                         WHERE b2."BookAuthorId" = a0."AuthorId"
                     ) AS "25"
                     FROM "Authors" AS a0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
        Assert.Single(command.Parameters);
        Assert.Equal(", ", command.Parameters[0].Value);
    }
#endif

#if !SQLITECIPHER
    [Fact]
    public void StringJoin_SubqueryWithOrderBy_ReturnsOrderedConcat()
    {
        using TestDatabase db = new();

        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "Anna", Email = "a@example.com", BirthDate = new DateTime(1980, 1, 1) });
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Beta", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Alpha", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "Gamma", AuthorId = 1, Price = 3 },
        });

        string row = db.Table<Author>()
            .Select(a => string.Join(", ", db.Table<Book>()
                .Where(b => b.AuthorId == a.Id)
                .Select(b => b.Title)
                .OrderBy(t => t)))
            .First()!;

        Assert.Equal("Alpha, Beta, Gamma", row);
    }

    [Fact]
    public void StringJoin_RootCall_WithOrderBy_ReturnsOrderedConcat()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Beta", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Alpha", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "Gamma", AuthorId = 1, Price = 3 },
        });

        string result = db.Table<Book>()
            .OrderBy(b => b.Title)
            .Select(b => b.Title)
            .StringJoin(", ");

        Assert.Equal("Alpha, Beta, Gamma", result);
    }
#endif

#if !SQLITE_FRAMEWORK_BUNDLED && !SQLITECIPHER && !NO_SQLITEPCL_RAW_BATTERIES
    [Fact]
    public void StringJoin_OrderBy_RequiresMinimumVersion3_44()
    {
        using TestDatabase db = new(opts => opts.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_43));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .OrderBy(b => b.Title)
                .Select(b => b.Title)
                .StringJoin(", "));

        Assert.Contains("3.44", ex.Message);
    }
#endif

    [Fact]
    public void StringJoin_RootCall_ReturnsConcatenated()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Alpha", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Beta", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "Gamma", AuthorId = 2, Price = 3 },
        });

        string result = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => b.Title)
            .StringJoin(", ");

        Assert.Equal("Alpha, Beta, Gamma", result);
    }

    [Fact]
    public void StringJoin_RootCall_OverIntColumn_ReturnsConcatenated()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 10, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 20, Title = "B", AuthorId = 1, Price = 2 },
        });

        string result = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .StringJoin("-");

        Assert.Equal("10-20", result);
    }

    [Fact]
    public void StringJoin_RootCall_NoRows_ReturnsEmptyString()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();

        string result = db.Table<Book>().Select(b => b.Title).StringJoin(", ");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void StringJoin_RootCall_WithWhere_FiltersBeforeAggregating()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Cheap", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Mid", AuthorId = 1, Price = 5 },
            new Book { Id = 3, Title = "Pricey", AuthorId = 1, Price = 20 },
        });

        string result = db.Table<Book>()
            .Where(b => b.Price >= 5)
            .OrderBy(b => b.Id)
            .Select(b => b.Title)
            .StringJoin("|");

        Assert.Equal("Mid|Pricey", result);
    }

    [Fact]
    public void StringJoin_RootCall_WithDistinct_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Select(b => b.Title).Distinct().StringJoin(","));

        Assert.Contains("group_concat", ex.Message);
        Assert.Contains("DISTINCT", ex.Message);
    }

    [Fact]
    public void StringJoin_RootCall_NullSource_Throws()
    {
        IQueryable<string> source = null!;
        Assert.Throws<ArgumentNullException>(() => source.StringJoin(", "));
    }

    [Fact]
    public void StringJoin_RootCall_NullSeparator_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<ArgumentNullException>(() => db.Table<Book>().Select(b => b.Title).StringJoin(null!));
    }

    [Fact]
    public void StringJoin_RootCall_NonSqliteQueryable_Throws()
    {
        IQueryable<string> source = new[] { "a", "b" }.AsQueryable();
        Assert.Throws<InvalidOperationException>(() => source.StringJoin(", "));
    }

    [Fact]
    public async Task StringJoinAsync_RootCall_ReturnsConcatenated()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Alpha", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Beta", AuthorId = 1, Price = 2 },
        });

        string result = await db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => b.Title)
            .StringJoinAsync(", ", TestContext.Current.CancellationToken);

        Assert.Equal("Alpha, Beta", result);
    }

    [Fact]
    public async Task StringJoinAsync_RootCall_NonSqliteQueryable_Throws()
    {
        IQueryable<string> source = new[] { "a", "b" }.AsQueryable();
        await Assert.ThrowsAsync<InvalidOperationException>(() => source.StringJoinAsync(", ", TestContext.Current.CancellationToken));
    }

    [Fact]
    public void GroupConcatMarker_DirectCall_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            ((Func<string>)(() =>
            {
                MethodInfo mi = typeof(QueryableExtensions).GetMethod(
                    "GroupConcatMarker",
                    BindingFlags.Static | BindingFlags.NonPublic)!;
                MethodInfo closed = mi.MakeGenericMethod(typeof(string));
                try
                {
                    return (string)closed.Invoke(null, [null!, ", "])!;
                }
                catch (TargetInvocationException tie) when (tie.InnerException != null)
                {
                    throw tie.InnerException;
                }
            }))());

        Assert.Contains("marker", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
