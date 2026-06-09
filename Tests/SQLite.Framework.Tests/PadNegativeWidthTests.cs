using System;
using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PadNegativeWidthTests
{
    private static TestDatabase SeedTitle(string title)
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = title, AuthorId = 1, Price = 1 });
        return db;
    }

    [Fact]
    public void PadLeft_NegativeWidth_DivergesFromDotNet()
    {
        using TestDatabase db = SeedTitle("abc");

        Assert.Throws<ArgumentOutOfRangeException>(() => "abc".PadLeft(-1));

        string actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.PadLeft(-1)).First();

        Assert.Equal("abc", actual);
    }

    [Fact]
    public void PadRight_NegativeWidth_DivergesFromDotNet()
    {
        using TestDatabase db = SeedTitle("abc");

        Assert.Throws<ArgumentOutOfRangeException>(() => "abc".PadRight(-1));

        string actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.PadRight(-1)).First();

        Assert.Equal("abc", actual);
    }
}