using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class NumericParseBugTests
{
    [Fact]
    public void IntParse_NumericPrefix_ThrowsFormatExceptionLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "12abc", AuthorId = 1, Price = 10.0 });

        Assert.Throws<FormatException>(() =>
            db.Table<Book>().Select(b => int.Parse(b.Title)).First());
    }

    [Fact]
    public void IntParse_NonNumeric_ThrowsFormatExceptionLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "abc", AuthorId = 1, Price = 10.0 });

        Assert.Throws<FormatException>(() =>
            db.Table<Book>().Select(b => int.Parse(b.Title)).First());
    }

    [Fact]
    public void DoubleParse_TrailingGarbage_ThrowsFormatExceptionLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "3.14xyz", AuthorId = 1, Price = 10.0 });

        Assert.Throws<FormatException>(() =>
            db.Table<Book>().Select(b => double.Parse(b.Title)).First());
    }
}
