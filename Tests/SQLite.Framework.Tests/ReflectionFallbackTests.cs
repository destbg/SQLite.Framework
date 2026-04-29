using SQLite.Framework.Tests.Entities;
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR
using SQLite.Framework.Generated;
#endif

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

        db.Table<Book>().Schema.CreateTable();
        db.Execute("INSERT INTO Books (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (1, 'A', 1, 10)");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Table<Book>().Select(b => new { b.Id, b.Title }).ToList());

        Assert.Contains("ReflectionFallbackDisabled", ex.Message);
    }

    [Fact]
    public void ThrowsWhenEntityWouldFallBackToReflection()
    {
        using SQLiteDatabase db = CreateDatabase(disableFallback: true);

        db.Table<Book>().Schema.CreateTable();
        db.Execute("INSERT INTO Books (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (1, 'A', 1, 10)");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Table<Book>().ToList());

        Assert.Contains("ReflectionFallbackDisabled", ex.Message);
    }

    [Fact]
    public void DoesNotThrowWhenFallbackIsAllowed()
    {
        using SQLiteDatabase db = CreateDatabase(disableFallback: false);

        db.Table<Book>().Schema.CreateTable();
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

    [Fact]
    public void ThrowsWhenGroupByWouldUseReflectionForKeySelector()
    {
        using SQLiteDatabase db = CreateDatabase(disableFallback: true);

        db.Table<Book>().Schema.CreateTable();
        db.Execute("INSERT INTO Books (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (1, 'A', 1, 10)");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Table<Book>().GroupBy(b => b.AuthorId).ToList());

        Assert.Contains("ReflectionFallbackDisabled", ex.Message);
        Assert.Contains("GroupBy", ex.Message);
    }

#if SQLITE_FRAMEWORK_SOURCE_GENERATOR
    [Fact]
    public void GroupByWorksUnderStrictModeWithGeneratedMaterializer()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(databasePath)
            .UseGeneratedMaterializers()
            .DisableReflectionFallback()
            .Build();
        using SQLiteDatabase db = new(options);

        db.Table<Book>().Schema.CreateTable();
        db.Execute("INSERT INTO Books (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (1, 'A', 1, 10)");
        db.Execute("INSERT INTO Books (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (2, 'B', 1, 20)");
        db.Execute("INSERT INTO Books (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (3, 'C', 2, 30)");

        Dictionary<int, List<Book>> byAuthor = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        Assert.Equal(2, byAuthor.Count);
        Assert.Equal(2, byAuthor[1].Count);
        Assert.Single(byAuthor[2]);
    }
#endif
}
