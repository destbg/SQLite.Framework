using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringSearchSemanticsTests
{
    private static TestDatabase SeedTitle(string title, Action<SQLiteOptionsBuilder>? configure = null)
    {
        TestDatabase db = new(configure);
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = title, AuthorId = 1, Price = 1 });
        return db;
    }

    [Fact]
    public void ContainsIsCaseSensitiveWithOption()
    {
        using TestDatabase db = SeedTitle("Hello World", b => b.UseCaseSensitiveStringComparison());

        List<Book> rows = db.Table<Book>().Where(b => b.Title.Contains("WORLD")).ToList();

        Assert.False("Hello World".Contains("WORLD"));
        Assert.Empty(rows);
    }

    [Fact]
    public void ContainsCaseSensitiveOptionStillMatchesSameCase()
    {
        using TestDatabase db = SeedTitle("Hello World", b => b.UseCaseSensitiveStringComparison());

        List<Book> rows = db.Table<Book>().Where(b => b.Title.Contains("World")).ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void StartsWithIsCaseSensitiveWithOption()
    {
        using TestDatabase db = SeedTitle("Hello World", b => b.UseCaseSensitiveStringComparison());

        List<Book> noMatch = db.Table<Book>().Where(b => b.Title.StartsWith("hello")).ToList();
        List<Book> match = db.Table<Book>().Where(b => b.Title.StartsWith("Hello")).ToList();

        Assert.False("Hello World".StartsWith("hello"));
        Assert.Empty(noMatch);
        Assert.Single(match);
    }

    [Fact]
    public void EndsWithIsCaseSensitiveWithOption()
    {
        using TestDatabase db = SeedTitle("Hello World", b => b.UseCaseSensitiveStringComparison());

        List<Book> noMatch = db.Table<Book>().Where(b => b.Title.EndsWith("WORLD")).ToList();
        List<Book> match = db.Table<Book>().Where(b => b.Title.EndsWith("World")).ToList();

        Assert.Empty(noMatch);
        Assert.Single(match);
    }

    [Fact]
    public void OrdinalIgnoreCaseDiffersFromCaseSensitiveDefault()
    {
        using TestDatabase db = SeedTitle("Hello World", b => b.UseCaseSensitiveStringComparison());

        bool defaultMatch = db.Table<Book>().Any(b => b.Title.Contains("WORLD"));
        bool ignoreCaseMatch = db.Table<Book>().Any(b => b.Title.Contains("WORLD", StringComparison.OrdinalIgnoreCase));

        Assert.False(defaultMatch);
        Assert.True(ignoreCaseMatch);
    }

    [Fact]
    public void DefaultContainsStaysCaseInsensitive()
    {
        using TestDatabase db = SeedTitle("Hello World");

        List<Book> rows = db.Table<Book>().Where(b => b.Title.Contains("WORLD")).ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void ContainsCharMatchesSubstring()
    {
        using TestDatabase db = SeedTitle("abc");

        List<Book> rows = db.Table<Book>().Where(b => b.Title.Contains('b')).ToList();

        Assert.True("abc".Contains('b'));
        Assert.Single(rows);
    }

    [Fact]
    public void IndexOfWithStartIndexRespectsStartIndex()
    {
        using TestDatabase db = SeedTitle("banana");

        int sqlIndex = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.IndexOf("a", 3)).First();

        Assert.Equal("banana".IndexOf("a", 3), sqlIndex);
    }
}
