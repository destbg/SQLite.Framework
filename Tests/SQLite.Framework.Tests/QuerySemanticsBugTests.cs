using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class QuerySemanticsBugTests
{
    [Fact]
    public void TakeBeforeConcatIsRejected()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => b.Price).Take(3)
                .Concat(db.Table<Book>())
                .ToList());
    }

    [Fact]
    public void TakeBeforeUnionIsRejected()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => b.Price).Take(2)
                .Union(db.Table<Book>().Where(b => b.Id > 3))
                .ToList());
    }

    [Fact]
    public void NegativeSkipAfterTakeDoesNotInflateTake()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        List<Book> data = new();
        for (int i = 1; i <= 10; i++)
        {
            Book b = new() { Id = i, Title = "T" + i, AuthorId = 1, Price = i };
            data.Add(b);
            db.Table<Book>().Add(b);
        }

        int expected = data.OrderBy(b => b.Id).Take(5).Skip(-3).Count();
        int actual = db.Table<Book>().OrderBy(b => b.Id).Take(5).Skip(-3).ToList().Count;

        Assert.Equal(expected, actual);
    }
}
