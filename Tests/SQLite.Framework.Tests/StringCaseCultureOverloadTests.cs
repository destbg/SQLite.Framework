using System.Globalization;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringCaseCultureOverloadTests
{
    private static TestDatabase SeedTitle(string title)
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = title, AuthorId = 1, Price = 1 });
        return db;
    }

    [Fact]
    public void StringToLower_WithCultureInfo_InWhereClause_ThrowsNotSupported()
    {
        using TestDatabase db = SeedTitle("ABC");

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Where(b => b.Title.ToLower(CultureInfo.InvariantCulture) == "abc")
                .ToList());
    }

    [Fact]
    public void StringToUpper_WithCultureInfo_InWhereClause_ThrowsNotSupported()
    {
        using TestDatabase db = SeedTitle("abc");

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Where(b => b.Title.ToUpper(CultureInfo.InvariantCulture) == "ABC")
                .ToList());
    }

    [Fact]
    public void StringToLower_NoArguments_StillTranslates()
    {
        using TestDatabase db = SeedTitle("ABC");

        List<string> oracle = new[] { "ABC" }.Where(t => t.ToLower() == "abc").ToList();

        List<Book> actual = db.Table<Book>()
            .Where(b => b.Title.ToLower() == "abc")
            .ToList();

        Assert.Single(oracle);
        Assert.Single(actual);
    }
}
