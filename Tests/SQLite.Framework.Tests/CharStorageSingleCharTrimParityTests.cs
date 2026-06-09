using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CharStorageSingleCharTrimParityTests
{
    [Fact]
    public void TrimSingleChar_IntegerCharStorage_MatchesDotNet()
    {
        using TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "aaabcaaa", AuthorId = 1, Price = 1 });

        string expected = "aaabcaaa".Trim('a');
        string actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.Trim('a')).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrimStartSingleChar_IntegerCharStorage_MatchesDotNet()
    {
        using TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "xxxabc", AuthorId = 1, Price = 1 });

        string expected = "xxxabc".TrimStart('x');
        string actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.TrimStart('x')).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrimEndSingleChar_IntegerCharStorage_MatchesDotNet()
    {
        using TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "abcyyy", AuthorId = 1, Price = 1 });

        string expected = "abcyyy".TrimEnd('y');
        string actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.TrimEnd('y')).First();

        Assert.Equal(expected, actual);
    }
}
