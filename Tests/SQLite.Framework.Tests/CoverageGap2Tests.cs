using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CoverageGap2Tests
{
    [Fact]
    public void Upsert_OnConflictWithoutDoCall_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        Book book = new() { Title = "x", AuthorId = 1, Price = 1.0 };

        Assert.Throws<InvalidOperationException>(() =>
            db.Table<Book>().Upsert(book, c => c.OnConflict(b => b.Id)));
    }

    [Fact]
    public void NumericInt_UnsupportedInstanceMethod_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>().Where(b => b.AuthorId.CompareTo(1) > 0).ToList());
    }

    [Fact]
    public void NumericFloat_UnsupportedInstanceMethod_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>().Where(b => b.Price.CompareTo(1.0) > 0).ToList());
    }

    [Fact]
    public void String_PadLeft_SingleArg_TranslatesAndRoundTrips()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "abc", AuthorId = 1, Price = 1.0 });

        List<string> result = db.Table<Book>().Select(b => b.Title.PadLeft(6)).ToList();

        Assert.Single(result);
        Assert.Equal("   abc", result[0]);
    }

    [Fact]
    public void String_PadRight_SingleArg_TranslatesAndRoundTrips()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "abc", AuthorId = 1, Price = 1.0 });

        List<string> result = db.Table<Book>().Select(b => b.Title.PadRight(6)).ToList();

        Assert.Single(result);
        Assert.Equal("abc   ", result[0]);
    }

    [Fact]
    public void BeginTransactionAsync_WhenAlreadyHoldingLock_UsesSyncCreateSavepoint()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Run().GetAwaiter().GetResult();

        async Task Run()
        {
            using SQLiteTransaction outer = db.BeginTransaction();
            await using SQLiteTransaction inner = await db.BeginTransactionAsync();
            db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1.0 });
            inner.Commit();
            outer.Commit();
        }

        Assert.Equal(1, db.Table<Book>().Count());
    }

    [Fact]
    public void String_Contains_AllIgnoreCaseComparisons_Translate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Hello", AuthorId = 1, Price = 1.0 });

        List<Book> ordinalIgnore = db.Table<Book>()
            .Where(b => b.Title.Contains("HELLO", StringComparison.OrdinalIgnoreCase)).ToList();
        List<Book> currentIgnore = db.Table<Book>()
            .Where(b => b.Title.Contains("HELLO", StringComparison.CurrentCultureIgnoreCase)).ToList();
        List<Book> invariantIgnore = db.Table<Book>()
            .Where(b => b.Title.Contains("HELLO", StringComparison.InvariantCultureIgnoreCase)).ToList();

        Assert.Single(ordinalIgnore);
        Assert.Single(currentIgnore);
        Assert.Single(invariantIgnore);
    }

    [Fact]
    public void Window_FrameBoundary_PrecedingAndFollowing_TranslateAndRoundTrip()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        for (int i = 1; i <= 3; i++)
        {
            db.Table<Book>().Add(new Book { Id = i, Title = $"b{i}", AuthorId = 1, Price = i });
        }

        List<double> sums = db.Table<Book>()
            .Select(b => SQLiteWindowFunctions.Sum(b.Price)
                .Over()
                .OrderBy(b.Id)
                .Rows(SQLiteFrameBoundary.Preceding(1), SQLiteFrameBoundary.Following(1))
                .AsValue())
            .ToList();

        Assert.Equal(3, sums.Count);
        Assert.Equal(3.0, sums[0]);
        Assert.Equal(6.0, sums[1]);
        Assert.Equal(5.0, sums[2]);
    }

    [Fact]
    public void Window_Lag_OneArgAndTwoArgAndThreeArg_AllTranslate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        for (int i = 1; i <= 3; i++)
        {
            db.Table<Book>().Add(new Book { Id = i, Title = $"b{i}", AuthorId = 1, Price = i });
        }

        string sql1 = db.Table<Book>()
            .Select(b => SQLiteWindowFunctions.Lag(b.Price).OrderBy(b.Id)).ToSql();
        string sql2 = db.Table<Book>()
            .Select(b => SQLiteWindowFunctions.Lag(b.Price, 2).OrderBy(b.Id)).ToSql();
        string sql3 = db.Table<Book>()
            .Select(b => SQLiteWindowFunctions.Lag(b.Price, 1L, -1.0).OrderBy(b.Id)).ToSql();

        Assert.Contains("LAG(", sql1);
        Assert.Contains("LAG(", sql2);
        Assert.Contains("LAG(", sql3);
    }

    [Fact]
    public void Reverse_EmptyTable_ReturnsEmpty()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        List<int> ids = db.Table<Book>().Reverse().Select(b => b.Id).ToList();

        Assert.Empty(ids);
    }

    [Fact]
    public void FirstOrDefault_PredicateAndDefault_NoMatch_ReturnsDefault()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Book def = new() { Id = -1, Title = "default", AuthorId = 0, Price = 0 };
        Book result = db.Table<Book>().FirstOrDefault(b => b.Id == 999, def);

        Assert.Equal(-1, result.Id);
        Assert.Equal("default", result.Title);
    }

    [Fact]
    public void FirstOrDefault_PredicateAndDefault_Matches_ReturnsRow()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Book def = new() { Id = -1, Title = "default", AuthorId = 0, Price = 0 };
        Book result = db.Table<Book>().FirstOrDefault(b => b.Id == 1, def);

        Assert.Equal(1, result.Id);
        Assert.Equal("A", result.Title);
    }

    [Fact]
    public void SingleOrDefault_PredicateAndDefault_NoMatch_ReturnsDefault()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Book def = new() { Id = -1, Title = "default", AuthorId = 0, Price = 0 };
        Book result = db.Table<Book>().SingleOrDefault(b => b.Id == 999, def);

        Assert.Equal(-1, result.Id);
    }

    [Fact]
    public void FirstOrDefault_PredicateAndNonConstantDefault_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryable<Book> source = db.Table<Book>();

        System.Reflection.MethodInfo firstOrDefaultWithPredicateAndDefault = typeof(System.Linq.Queryable)
            .GetMethods()
            .First(m =>
                m.Name == nameof(System.Linq.Queryable.FirstOrDefault)
                && m.GetParameters().Length == 3
                && m.GetParameters()[1].ParameterType.IsGenericType
                && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(System.Linq.Expressions.Expression<>))
            .MakeGenericMethod(typeof(Book));

        System.Linq.Expressions.ParameterExpression p = System.Linq.Expressions.Expression.Parameter(typeof(Book), "b");
        System.Linq.Expressions.Expression<Func<Book, bool>> predicate = System.Linq.Expressions.Expression.Lambda<Func<Book, bool>>(
            System.Linq.Expressions.Expression.Equal(
                System.Linq.Expressions.Expression.Property(p, nameof(Book.Id)),
                System.Linq.Expressions.Expression.Constant(1)),
            p);

        System.Linq.Expressions.Expression nonConstantDefault = System.Linq.Expressions.Expression.Invoke(
            System.Linq.Expressions.Expression.Lambda<Func<Book>>(System.Linq.Expressions.Expression.New(typeof(Book))));

        System.Linq.Expressions.MethodCallExpression call = System.Linq.Expressions.Expression.Call(
            firstOrDefaultWithPredicateAndDefault,
            source.Expression,
            System.Linq.Expressions.Expression.Quote(predicate),
            nonConstantDefault);

        Assert.Throws<NotSupportedException>(() => source.Provider.Execute<Book>(call));
    }

    [Fact]
    public void IQueryProvider_ExecuteIEnumerable_EmptyTable_ReturnsEmpty()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryable<Book> source = db.Table<Book>();
        System.Collections.IEnumerable result = source.Provider.Execute<System.Collections.IEnumerable>(source.Expression);

        int count = 0;
        foreach (object _ in result)
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public void Reverse_BeforeFirst_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Reverse().First());
        Assert.Contains("First after Reverse is not supported", ex.Message);
    }

    [Fact]
    public void Reverse_BeforeFirstOrDefault_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Reverse().FirstOrDefault());
    }

    [Fact]
    public void Reverse_BeforeSingle_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Reverse().Single());
    }

    [Fact]
    public void Reverse_BeforeSingleOrDefault_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Reverse().SingleOrDefault());
    }

    [Fact]
    public void Reverse_BeforeTake_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Reverse().Take(2).ToList());
        Assert.Contains("Take after Reverse is not supported", ex.Message);
    }

    [Fact]
    public void Reverse_BeforeSkip_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Reverse().Skip(1).ToList());
        Assert.Contains("Skip after Reverse is not supported", ex.Message);
    }

    [Fact]
    public void DoubleReverse_BeforeFirst_Allowed()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Book row = db.Table<Book>().OrderBy(b => b.Id).Reverse().Reverse().First();

        Assert.Equal(1, row.Id);
    }

    [Fact]
    public void Select_AnonymousType_PositionalConstructor_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 2, Price = 1.0 });

        var rows = db.Table<Book>()
            .Select(b => new { b.Id, b.Title })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(1, rows[0].Id);
        Assert.Equal("x", rows[0].Title);
    }
}
