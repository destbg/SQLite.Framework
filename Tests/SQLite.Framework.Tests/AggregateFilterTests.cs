using System.Linq.Expressions;
using System.Reflection;
#if !SQLITE_FRAMEWORK_BUNDLED && !SQLITECIPHER && !NO_SQLITEPCL_RAW_BATTERIES
using SQLite.Framework.Enums;
#endif
using SQLite.Framework.Extensions;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Internals.Visitors.Member;
using SQLite.Framework.Internals.Visitors.SQL;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AggregateFilterTests
{
    [Fact]
    public void GroupBySum_WithFilter_EmitsFilterClause()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select new
            {
                AuthorId = g.Key,
                Pricey = g.Where(x => x.Price >= 10).Sum(x => x.Price)
            }
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(10.0, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0."BookAuthorId" AS "AuthorId",
                            COALESCE(SUM(b0."BookPrice") FILTER (WHERE b0."BookPrice" >= @p0), 0) AS "Pricey"
                     FROM "Books" AS b0
                     GROUP BY b0."BookAuthorId"
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupBySum_FilterAndPlainAggregate_BothEmitted()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 12 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 20 },
        });

        var rows = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select new
            {
                AuthorId = g.Key,
                Pricey = g.Where(x => x.Price >= 10).Sum(x => x.Price),
                Total = g.Sum(x => x.Price)
            }
        ).ToList();

        Assert.Single(rows);
        Assert.Equal(1, rows[0].AuthorId);
        Assert.Equal(32.0, rows[0].Pricey);
        Assert.Equal(33.0, rows[0].Total);
    }

    [Fact]
    public void GroupByAverage_WithFilter_ComputesAvgOverFilteredRows()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 2 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 10 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 20 },
        });

        double avg = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Where(x => x.Price >= 10).Average(x => x.Price)
        ).Single();

        Assert.Equal(15.0, avg);
    }

    [Fact]
    public void GroupByMin_WithFilter_ReturnsMinOverFilteredRows()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 2 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 10 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 20 },
        });

        double min = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Where(x => x.Price >= 10).Min(x => x.Price)
        ).Single();

        Assert.Equal(10.0, min);
    }

    [Fact]
    public void GroupByMax_WithFilter_ReturnsMaxOverFilteredRows()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 2 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 10 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 50 },
        });

        double max = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Where(x => x.Price < 50).Max(x => x.Price)
        ).Single();

        Assert.Equal(10.0, max);
    }

    [Fact]
    public void GroupByCount_WithFilter_ReturnsFilteredCount()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 11 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 22 },
        });

        int count = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Where(x => x.Price >= 10).Count()
        ).Single();

        Assert.Equal(2, count);
    }

    [Fact]
    public void GroupByLongCount_WithFilter_ReturnsFilteredCount()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 11 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 22 },
        });

        long count = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Where(x => x.Price >= 10).LongCount()
        ).Single();

        Assert.Equal(2L, count);
    }

    [Fact]
    public void GroupByCount_WithFilter_EmitsCountStarFilter()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Where(x => x.Price >= 10).Count()
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(10.0, command.Parameters[0].Value);
        Assert.Equal("SELECT COUNT(*) FILTER (WHERE b0.\"BookPrice\" >= @p0) AS \"7\"\nFROM \"Books\" AS b0\nGROUP BY b0.\"BookAuthorId\"", command.CommandText);
    }

    [Fact]
    public void GroupBySum_FilterMatchesNoRows_ReturnsZero()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        });

        double? filteredSum = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Where(x => x.Price > 10000).Sum(x => (double?)x.Price)
        ).Single();

        Assert.Equal(0.0, filteredSum);
    }

    [Fact]
    public void GroupByTotal_FilterMatchesNoRows_ReturnsZero()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        });

        double total = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select SQLiteFunctions.Total(g.Where(x => x.Price > 10000).Select(x => x.Price))
        ).Single();

        Assert.Equal(0.0, total);
    }

    [Fact]
    public void GroupBySum_FilterMatchesAllRows_EqualsPlainSum()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 5 },
        });

        var rows = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select new
            {
                Filtered = g.Where(x => x.Price >= 0).Sum(x => x.Price),
                Plain = g.Sum(x => x.Price)
            }
        ).Single();

        Assert.Equal(rows.Plain, rows.Filtered);
    }

    [Fact]
    public void GroupByCount_FilterMatchesNoRows_ReturnsZero()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        });

        int count = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Where(x => x.Price > 10000).Count()
        ).Single();

        Assert.Equal(0, count);
    }

    [Fact]
    public void GroupByCount_FilterMatchesAllRows_EqualsPlainCount()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        });

        var row = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select new
            {
                Filtered = g.Where(x => x.Price >= 0).Count(),
                Plain = g.Count()
            }
        ).Single();

        Assert.Equal(row.Plain, row.Filtered);
    }

    [Fact]
    public void GroupByTotal_WithFilter_EmitsTotalFilterClause()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select SQLiteFunctions.Total(g.Where(x => x.Price >= 10).Select(x => x.Price))
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(10.0, command.Parameters[0].Value);
        Assert.Equal("SELECT total(b0.\"BookPrice\") FILTER (WHERE b0.\"BookPrice\" >= @p0) AS \"7\"\nFROM \"Books\" AS b0\nGROUP BY b0.\"BookAuthorId\"", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByTotal_WithFilter_ReturnsFilteredTotal()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 11 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 22 },
        });

        double sum = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select SQLiteFunctions.Total(g.Where(x => x.Price >= 10).Select(x => x.Price))
        ).Single();

        Assert.Equal(33.0, sum);
    }

    [Fact]
    public void Total_IndexedSelect_Throws()
    {
        using TestDatabase db = new();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            (
                from book in db.Table<Book>()
                group book by book.AuthorId
                into g
                select SQLiteFunctions.Total(g.Select((x, i) => x.Price))
            ).ToSqlCommand());

        Assert.Contains("Select projection over a grouping", ex.Message);
    }

    [Fact]
    public void Total_SelectOverNonGroupingChain_Throws()
    {
        using TestDatabase db = new();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            (
                from book in db.Table<Book>()
                group book by book.AuthorId
                into g
                select SQLiteFunctions.Total(g.OrderBy(x => x.Price).Select(x => x.Price))
            ).ToSqlCommand());

        Assert.Contains("Select projection over a grouping", ex.Message);
    }

    [Fact]
    public void Total_NonSelectMethodCall_Throws()
    {
        using TestDatabase db = new();

        int[] arr = [1, 2, 3];
        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            (
                from book in db.Table<Book>()
                group book by book.AuthorId
                into g
                select SQLiteFunctions.Total(arr.Reverse())
            ).ToSqlCommand());

        Assert.Contains("Select projection over a grouping", ex.Message);
    }

    [Fact]
    public void Aggregate_IndexedWhereOnGrouping_NotPeeledAsFilter()
    {
        using TestDatabase db = new();

        Assert.ThrowsAny<NotSupportedException>(() =>
            (
                from book in db.Table<Book>()
                group book by book.AuthorId
                into g
                select g.Where((x, i) => i > 0).Sum(x => x.Price)
            ).ToSqlCommand());
    }

    [Fact]
    public void Total_SelectReceiverNotGrouping_Throws()
    {
        using TestDatabase db = new();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            (
                from book in db.Table<Book>()
                group book by book.AuthorId
                into g
                select SQLiteFunctions.Total(new[] { 1, 2, 3 }.Select(x => x))
            ).ToSqlCommand());

        Assert.Contains("Select projection over a grouping", ex.Message);
    }

    [Fact]
    public void HandleGroupingMethod_FilterPredicateNotSql_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression groupingParam = Expression.Parameter(typeof(IGrouping<int, Book>), "g");
        sqlVisitor.MethodArguments[groupingParam] = new Dictionary<string, Expression>
        {
            ["Key"] = SQLiteExpression.Leaf(typeof(int), 0, "b0.Id")
        };

        ParameterExpression bookParam = Expression.Parameter(typeof(Book), "x");
        LambdaExpression predicate = Expression.Lambda(Expression.Default(typeof(bool)), bookParam);

        MethodInfo whereMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Where)
                && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType.IsGenericType
                && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(typeof(Book));
        MethodCallExpression whereCall = Expression.Call(whereMethod, groupingParam, predicate);

        MethodInfo countMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Count)
                && m.GetParameters().Length == 1)
            .MakeGenericMethod(typeof(Book));
        MethodCallExpression countCall = Expression.Call(countMethod, whereCall);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            QueryableMemberVisitor.HandleGroupingMethod(sqlVisitor, countCall));

        Assert.Contains("FILTER predicate could not be resolved", ex.Message);
    }

    [Fact]
    public void HandleFunctionsTotal_FilterPredicateNotSql_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression groupingParam = Expression.Parameter(typeof(IGrouping<int, Book>), "g");
        sqlVisitor.MethodArguments[groupingParam] = new Dictionary<string, Expression>
        {
            ["Key"] = SQLiteExpression.Leaf(typeof(int), 0, "b0.Id"),
            ["Price"] = SQLiteExpression.Leaf(typeof(double), 1, "b0.Price")
        };

        ParameterExpression bookParam = Expression.Parameter(typeof(Book), "x");
        LambdaExpression predicate = Expression.Lambda(Expression.Default(typeof(bool)), bookParam);

        MethodInfo whereMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Where)
                && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType.IsGenericType
                && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(typeof(Book));
        MethodCallExpression whereCall = Expression.Call(whereMethod, groupingParam, predicate);

        ParameterExpression selectParam = Expression.Parameter(typeof(Book), "y");
        LambdaExpression selector = Expression.Lambda(
            Expression.Property(selectParam, nameof(Book.Price)),
            selectParam);

        MethodInfo selectMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Select)
                && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType.IsGenericType
                && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(typeof(Book), typeof(double));
        MethodCallExpression selectCall = Expression.Call(selectMethod, whereCall, selector);

        MethodInfo totalMethod = typeof(SQLiteFunctions).GetMethod(nameof(SQLiteFunctions.Total))!
            .MakeGenericMethod(typeof(double));
        MethodCallExpression totalCall = Expression.Call(totalMethod, selectCall);

        SQLiteCallerContext ctx = new(sqlVisitor, totalCall);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            SQLiteFunctionsMemberVisitor.HandleSQLiteFunctionsMethod(ctx));

        Assert.Contains("FILTER predicate could not be resolved", ex.Message);
    }

#if !SQLITE_FRAMEWORK_BUNDLED && !SQLITECIPHER && !NO_SQLITEPCL_RAW_BATTERIES
    [Fact]
    public void AggregateFilter_RequiresMinimumVersion3_30()
    {
        using TestDatabase db = new(opts => opts.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_29));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            (
                from book in db.Table<Book>()
                group book by book.AuthorId
                into g
                select g.Where(x => x.Price >= 10).Sum(x => x.Price)
            ).ToSqlCommand());

        Assert.Contains("3.30", ex.Message);
    }

    [Fact]
    public void PlainAggregate_WithoutFilter_DoesNotRequireMinimumVersion3_30()
    {
        using TestDatabase db = new(opts => opts.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_29));

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Sum(x => x.Price)
        ).ToSqlCommand();

        Assert.Equal("SELECT COALESCE(SUM(b0.\"BookPrice\"), 0) AS \"5\"\nFROM \"Books\" AS b0\nGROUP BY b0.\"BookAuthorId\"", command.CommandText);
    }
#endif
}
