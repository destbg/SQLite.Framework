using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CharStorageLastIndexOfParityTests
{
    [Fact]
    public void LastIndexOfChar_IntegerCharStorage_MatchesDotNet()
    {
        using TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "hello", AuthorId = 1, Price = 1 });

        int expected = "hello".LastIndexOf('l');
        int actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.LastIndexOf('l')).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LastIndexOfChar_NotFound_IntegerCharStorage_ReturnsMinusOne()
    {
        using TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "hello", AuthorId = 1, Price = 1 });

        int expected = "hello".LastIndexOf('z');
        int actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.LastIndexOf('z')).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LastIndexOfCharWithStartIndex_IntegerCharStorage_MatchesDotNet()
    {
        using TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "banana", AuthorId = 1, Price = 1 });

        (char needle, int start)[] cases = [('a', 3), ('a', 0), ('n', 2), ('a', 5), ('x', 4)];

        foreach ((char needle, int start) in cases)
        {
            int expected = "banana".LastIndexOf(needle, start);
            int actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.LastIndexOf(needle, start)).First();
            Assert.Equal(expected, actual);
        }
    }
}
