using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringReplaceNullParityTests
{
    [Fact]
    public void Replace_NullNewValue_RemovesOccurrences()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "hello world", AuthorId = 1, Price = 1 });

        string oracle = "hello world".Replace("l", null);
        string actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.Replace("l", (string?)null)).First();

        Assert.Equal(oracle, actual);
    }
}
