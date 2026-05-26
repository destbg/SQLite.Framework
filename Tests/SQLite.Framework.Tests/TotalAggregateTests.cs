using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Extensions;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Internals.Visitors.Member;
using SQLite.Framework.Internals.Visitors.Queryable;
using SQLite.Framework.Internals.Visitors.SQL;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TotalAggregateTests
{
    [Fact]
    public void GroupByTotal_EmitsTotalSql()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select SQLiteFunctions.Total(g.Select(x => x.Price))
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT total(b0.BookPrice) AS "5"
                     FROM "Books" AS b0
                     GROUP BY b0.BookAuthorId
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByTotal_OverIntColumn_ReturnsSumAsDouble()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 10, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 20, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 30, Title = "C", AuthorId = 2, Price = 3 },
        });

        List<double> rows = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            orderby g.Key
            select SQLiteFunctions.Total(g.Select(x => x.Id))
        ).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(30.0, rows[0]);
        Assert.Equal(30.0, rows[1]);
    }

    [Fact]
    public void GroupByTotal_OverDecimalColumn_ReturnsSumAsDouble()
    {
        using TestDatabase db = new();

        db.Table<ProductLine>().Schema.CreateTable();
        db.Execute(
            "INSERT INTO ProductLines (Id, Price, Quantity, Total) VALUES (1, 1.5, 2, 3.0), (2, 2.25, 1, 2.25), (3, 4.5, 1, 4.5)");

        List<double> rows = (
            from p in db.Table<ProductLine>()
            group p by p.Quantity
            into g
            orderby g.Key
            select SQLiteFunctions.Total(g.Select(x => x.Price))
        ).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(6.75, rows[0]);
        Assert.Equal(1.5, rows[1]);
    }

    [Fact]
    public void GroupByTotal_OverEmptyTable_ReturnsNoRows()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();

        List<double> rows = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select SQLiteFunctions.Total(g.Select(x => x.Price))
        ).ToList();

        Assert.Empty(rows);
    }

    [Fact]
    public void GroupByTotal_OverSingleRowGroup_ReturnsValue()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Only", AuthorId = 7, Price = 12.5 });

        List<double> rows = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select SQLiteFunctions.Total(g.Select(x => x.Price))
        ).ToList();

        Assert.Single(rows);
        Assert.Equal(12.5, rows[0]);
    }

    [Fact]
    public void GroupByTotal_InsideSelectNew_ReturnsKeyAndTotal()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        });

        var rows = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            orderby g.Key
            select new
            {
                AuthorId = g.Key,
                Revenue = SQLiteFunctions.Total(g.Select(x => x.Price))
            }
        ).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0].AuthorId);
        Assert.Equal(3.0, rows[0].Revenue);
        Assert.Equal(2, rows[1].AuthorId);
        Assert.Equal(3.0, rows[1].Revenue);
    }

    [Fact]
    public void Total_OutsideSelectProjection_Throws()
    {
        using TestDatabase db = new();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            (
                from book in db.Table<Book>()
                group book by book.AuthorId
                into g
                select SQLiteFunctions.Total((IEnumerable<int>)new[] { 1, 2 })
            ).ToSqlCommand());

        Assert.Contains("Select", ex.Message);
    }

    [Fact]
    public void RootTotal_OverDoubleSelector_ReturnsSum()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1.5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2.25 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 4.5 },
        });

        double total = db.Table<Book>().Total(b => b.Price);

        Assert.Equal(8.25, total);
    }

    [Fact]
    public void RootTotal_OverDecimalSelector_ReturnsSum()
    {
        using TestDatabase db = new();

        db.Table<ProductLine>().Schema.CreateTable();
        db.Execute(
            "INSERT INTO ProductLines (Id, Price, Quantity, Total) VALUES (1, 1.5, 2, 3.0), (2, 2.25, 1, 2.25)");

        double total = db.Table<ProductLine>().Total(p => p.Price);

        Assert.Equal(3.75, total);
    }

    [Fact]
    public void RootTotal_OverIntSelector_ReturnsSum()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 4, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 5, Price = 2 },
        });

        double total = db.Table<Book>().Total(b => b.AuthorId);

        Assert.Equal(9.0, total);
    }

    [Fact]
    public void RootTotal_OverLongSelector_ReturnsSum()
    {
        using TestDatabase db = new();

        db.Table<NumericType>().Schema.CreateTable();
        db.Execute(
            "INSERT INTO NumericTypes (Id, IntValue, LongValue, ShortValue, ByteValue, SByteValue, UIntValue, ULongValue, UShortValue, DoubleValue, FloatValue, DecimalValue, CharValue) VALUES (1, 0, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), (2, 0, 250, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)");

        double total = db.Table<NumericType>().Total(n => n.LongValue);

        Assert.Equal(350.0, total);
    }

    [Fact]
    public void RootTotal_OverEmptyTable_ReturnsZero()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        double total = db.Table<Book>().Total(b => b.Price);

        Assert.Equal(0.0, total);
    }

    [Fact]
    public void RootTotal_EmitsTotalSql()
    {
        SqlCapture capture = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(capture));
        db.Table<Book>().Schema.CreateTable();
        capture.Reset();

        db.Table<Book>().Total(b => b.Price);

        Assert.Contains("total(b0.BookPrice)", capture.ExecutingTexts[0]);
        Assert.Contains("FROM \"Books\" AS b0", capture.ExecutingTexts[0]);
    }

    [Fact]
    public void RootTotal_WithWhere_FiltersBeforeAggregating()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 5 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 20 },
        });

        double total = db.Table<Book>().Where(b => b.Price >= 5).Total(b => b.Price);

        Assert.Equal(25.0, total);
    }

    [Fact]
    public void RootTotal_AfterTake_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Take(5).Total(b => b.Price));
    }

    [Fact]
    public void RootTotal_AfterSkip_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Skip(2).Total(b => b.Price));
    }

    [Fact]
    public void RootTotal_AfterConcat_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Concat(db.Table<Book>()).Total(b => b.Price));
    }

    [Fact]
    public void RootTotal_AfterDistinct_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Distinct().Total(b => b.Price));
    }

    [Fact]
    public void RootTotal_OverNonSqliteQueryable_Throws()
    {
        IQueryable<Book> source = new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        }.AsQueryable();

        Assert.Throws<InvalidOperationException>(() => source.Total(b => b.Price));
    }

    [Fact]
    public void RootTotal_NullSource_Throws()
    {
        IQueryable<Book> source = null!;
        Assert.Throws<ArgumentNullException>(() => source.Total(b => b.Price));
    }

    [Fact]
    public void RootTotal_NullSelector_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<ArgumentNullException>(() => db.Table<Book>().Total((Expression<Func<Book, double>>)null!));
    }

    [Fact]
    public async Task RootTotalAsync_OverDoubleSelector_ReturnsSum()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1.5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2.5 },
        });

        double total = await db.Table<Book>().TotalAsync(b => b.Price, TestContext.Current.CancellationToken);

        Assert.Equal(4.0, total);
    }

    [Fact]
    public async Task RootTotalAsync_OverDecimalSelector_ReturnsSum()
    {
        using TestDatabase db = new();
        db.Table<ProductLine>().Schema.CreateTable();
        db.Execute(
            "INSERT INTO ProductLines (Id, Price, Quantity, Total) VALUES (1, 1.5, 2, 3.0), (2, 2.5, 1, 2.5)");

        double total = await db.Table<ProductLine>().TotalAsync(p => p.Price, TestContext.Current.CancellationToken);

        Assert.Equal(4.0, total);
    }

    [Fact]
    public async Task RootTotalAsync_OverIntSelector_ReturnsSum()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 3, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 7, Price = 2 },
        });

        double total = await db.Table<Book>().TotalAsync(b => b.AuthorId, TestContext.Current.CancellationToken);

        Assert.Equal(10.0, total);
    }

    [Fact]
    public async Task RootTotalAsync_OverLongSelector_ReturnsSum()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Execute(
            "INSERT INTO NumericTypes (Id, IntValue, LongValue, ShortValue, ByteValue, SByteValue, UIntValue, ULongValue, UShortValue, DoubleValue, FloatValue, DecimalValue, CharValue) VALUES (1, 0, 50, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)");

        double total = await db.Table<NumericType>().TotalAsync(n => n.LongValue, TestContext.Current.CancellationToken);

        Assert.Equal(50.0, total);
    }

    [Fact]
    public async Task RootTotalAsync_OverNonSqliteQueryable_Throws()
    {
        IQueryable<Book> source = Array.Empty<Book>().AsQueryable();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.TotalAsync(b => b.Price, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.TotalAsync((Expression<Func<Book, decimal>>)(b => (decimal)b.Price), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.TotalAsync((Expression<Func<Book, int>>)(b => b.AuthorId), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.TotalAsync((Expression<Func<Book, long>>)(b => (long)b.Id), TestContext.Current.CancellationToken));
    }

    private sealed class SqlCapture : ISQLiteCommandInterceptor
    {
        public List<string> ExecutingTexts { get; } = [];

        public void Reset()
        {
            ExecutingTexts.Clear();
        }

        public void OnExecuting(SQLiteCommand command)
        {
            ExecutingTexts.Add(command.CommandText);
        }

        public void OnExecuted(SQLiteCommand command, int? rowsAffected)
        {
        }

        public void OnFailed(SQLiteCommand command, Exception exception)
        {
        }
    }

    [Fact]
    public void HandleFunctionsTotal_LambdaBodyNotSql_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression groupingParam = Expression.Parameter(typeof(IGrouping<int, Book>), "g");
        sqlVisitor.MethodArguments[groupingParam] = new Dictionary<string, Expression>
        {
            ["Key"] = SQLiteExpression.Leaf(typeof(int), 0, "b0.Id")
        };

        ParameterExpression bookParam = Expression.Parameter(typeof(Book), "x");
        LambdaExpression selector = Expression.Lambda(Expression.Default(typeof(int)), bookParam);

        MethodInfo selectMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Select)
                && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType.IsGenericType
                && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(typeof(Book), typeof(int));
        MethodCallExpression selectCall = Expression.Call(selectMethod, groupingParam, selector);

        MethodInfo totalMethod = typeof(SQLiteFunctions).GetMethod(nameof(SQLiteFunctions.Total))!
            .MakeGenericMethod(typeof(int));
        MethodCallExpression totalCall = Expression.Call(totalMethod, selectCall);

        SQLiteCallerContext ctx = new(sqlVisitor, totalCall);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            SQLiteFunctionsMemberVisitor.HandleSQLiteFunctionsMethod(ctx));

        Assert.Contains("could not resolve", ex.Message);
    }

    [Fact]
    public void VisitTotal_LambdaBodyNotSql_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);
        QueryableVisitor qmv = new(db, sqlVisitor);

        ParameterExpression bookParam = Expression.Parameter(typeof(Book), "x");
        LambdaExpression selector = Expression.Lambda(Expression.Default(typeof(double)), bookParam);

        ConstantExpression source = Expression.Constant(db.Table<Book>(), typeof(IQueryable<Book>));

        MethodInfo marker = typeof(QueryableExtensions)
            .GetMethod("TotalMarker", BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(Book), typeof(double));
        MethodCallExpression totalCall = Expression.Call(marker, source, selector);

        MethodInfo visitTotal = typeof(QueryableVisitor)
            .GetMethod("VisitTotal", BindingFlags.Instance | BindingFlags.NonPublic)!;

        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() =>
            visitTotal.Invoke(qmv, [totalCall]));

        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("Unsupported Total expression", tie.InnerException!.Message);
    }

    [Fact]
    public void TotalMarker_DirectCall_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            ((Func<double>)(() =>
            {
                MethodInfo mi = typeof(QueryableExtensions).GetMethod(
                    "TotalMarker",
                    BindingFlags.Static | BindingFlags.NonPublic)!;
                MethodInfo closed = mi.MakeGenericMethod(typeof(Book), typeof(double));
                try
                {
                    return (double)closed.Invoke(null, [null!, null!])!;
                }
                catch (TargetInvocationException tie) when (tie.InnerException != null)
                {
                    throw tie.InnerException;
                }
            }))());

        Assert.Contains("marker", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
