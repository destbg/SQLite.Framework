using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class QuerySemanticsTests
{
    [Fact]
    public void TakeBeforeConcatLimitsOnlyTheFirstOperand()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        Book[] books =
        [
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 3 },
            new Book { Id = 2, Title = "T2", AuthorId = 1, Price = 1 },
            new Book { Id = 3, Title = "T3", AuthorId = 1, Price = 2 }
        ];
        db.Table<Book>().AddRange(books);

        List<int> expected = books.OrderBy(b => b.Price).Take(2).Concat(books).Select(b => b.Id).OrderBy(x => x).ToList();
        List<int> actual = db.Table<Book>().OrderBy(b => b.Price).Take(2)
            .Concat(db.Table<Book>())
            .Select(b => b.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal([1, 2, 2, 3, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TakeBeforeUnionLimitsOnlyTheFirstOperand()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        Book[] books =
        [
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 3 },
            new Book { Id = 2, Title = "T2", AuthorId = 1, Price = 1 },
            new Book { Id = 3, Title = "T3", AuthorId = 1, Price = 2 },
            new Book { Id = 4, Title = "T4", AuthorId = 1, Price = 4 }
        ];
        db.Table<Book>().AddRange(books);

        List<int> expected = books.OrderBy(b => b.Price).Take(2)
            .Union(books.Where(b => b.Id > 3))
            .Select(b => b.Id)
            .OrderBy(x => x)
            .ToList();
        List<int> actual = db.Table<Book>().OrderBy(b => b.Price).Take(2)
            .Union(db.Table<Book>().Where(b => b.Id > 3))
            .Select(b => b.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal([2, 3, 4], expected);
        Assert.Equal(expected, actual);
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
