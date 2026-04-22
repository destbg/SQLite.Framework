using SQLite.Framework.Tests.Entities;

namespace SQLite.Framework.Tests;

public class ReflectionFallbackTests : IDisposable
{
    private readonly string databasePath = $"ReflectionFallback_{Guid.NewGuid():N}.db3";

    public void Dispose()
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    private SQLiteDatabase CreateDatabase(bool disableFallback)
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(databasePath)
            .DisableReflectionFallback(disableFallback)
            .Build();
        return new SQLiteDatabase(options);
    }

    [Fact]
    public void ThrowsWhenSelectWouldFallBackToReflection()
    {
        using SQLiteDatabase db = CreateDatabase(disableFallback: true);

        db.Table<Book>().CreateTable();
        db.Execute("INSERT INTO Books (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (1, 'A', 1, 10)");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Table<Book>().Select(b => new { b.Id, b.Title }).ToList());

        Assert.Contains("ReflectionFallbackDisabled", ex.Message);
    }

    [Fact]
    public void ThrowsWhenEntityWouldFallBackToReflection()
    {
        using SQLiteDatabase db = CreateDatabase(disableFallback: true);

        db.Table<Book>().CreateTable();
        db.Execute("INSERT INTO Books (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (1, 'A', 1, 10)");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Table<Book>().ToList());

        Assert.Contains("ReflectionFallbackDisabled", ex.Message);
    }

    [Fact]
    public void DoesNotThrowWhenFallbackIsAllowed()
    {
        using SQLiteDatabase db = CreateDatabase(disableFallback: false);

        db.Table<Book>().CreateTable();
        db.Execute("INSERT INTO Books (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (1, 'A', 1, 10)");

        List<Book> books = db.Table<Book>().ToList();

        Assert.Single(books);
    }

    [Fact]
    public void OptionsBuilderDefaultsToFallbackEnabled()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder("noop.db3").Build();
        Assert.False(options.ReflectionFallbackDisabled);
    }

    [Fact]
    public void OptionsBuilderSetsFlag()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder("noop.db3")
            .DisableReflectionFallback()
            .Build();
        Assert.True(options.ReflectionFallbackDisabled);
    }
}
