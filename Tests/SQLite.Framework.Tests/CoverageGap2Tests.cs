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
                .Rows(SQLiteFrameBoundary.Preceding(1), SQLiteFrameBoundary.Following(1)))
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
            .Select(b => SQLiteWindowFunctions.Lag(b.Price).Over().OrderBy(b.Id)).ToSql();
        string sql2 = db.Table<Book>()
            .Select(b => SQLiteWindowFunctions.Lag(b.Price, 2).Over().OrderBy(b.Id)).ToSql();
        string sql3 = db.Table<Book>()
            .Select(b => SQLiteWindowFunctions.Lag(b.Price, 1L, -1.0).Over().OrderBy(b.Id)).ToSql();

        Assert.Contains("LAG(", sql1);
        Assert.Contains("LAG(", sql2);
        Assert.Contains("LAG(", sql3);
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
