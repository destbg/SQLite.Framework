using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CharStorageIntegerStringMethodParityTests
{
    [Fact]
    public void IndexOfChar_IntegerCharStorage_MatchesDotNet()
    {
        using TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "hello", AuthorId = 1, Price = 1 });

        int expected = "hello".IndexOf('l');
        int actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.IndexOf('l')).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PadLeftDefaultSpace_IntegerCharStorage_MatchesDotNet()
    {
        using TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "ab", AuthorId = 1, Price = 1 });

        string expected = "ab".PadLeft(5);
        string actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.PadLeft(5)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PadLeftWithChar_IntegerCharStorage_MatchesDotNet()
    {
        using TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "ab", AuthorId = 1, Price = 1 });

        string expected = "ab".PadLeft(5, '*');
        string actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.PadLeft(5, '*')).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrimStartCharArray_IntegerCharStorage_MatchesDotNet()
    {
        using TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "xyab", AuthorId = 1, Price = 1 });

        string expected = "xyab".TrimStart('x', 'y');
        string actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.TrimStart('x', 'y')).First();

        Assert.Equal(expected, actual);
    }
}
