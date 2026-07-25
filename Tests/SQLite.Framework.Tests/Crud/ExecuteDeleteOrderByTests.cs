using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ExecuteDeleteOrderByTests
{
    [Fact]
    public void ExecuteDeleteWithOrderByDeletesMatchingRows()
    {
        Book[] seed =
        {
            new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "c", AuthorId = 2, Price = 3 },
        };

        List<Book> memory = seed.OrderBy(b => b.Price).ToList();
        List<Book> matched = memory.Where(b => b.AuthorId == 1).ToList();
        int oracleDeleted = matched.Count;
        foreach (Book b in matched)
        {
            memory.Remove(b);
        }
        int oracleRemaining = memory.Count;

        Assert.Equal(2, oracleDeleted);
        Assert.Equal(1, oracleRemaining);

        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(seed.Select(b => new Book
        {
            Id = b.Id,
            Title = b.Title,
            AuthorId = b.AuthorId,
            Price = b.Price,
        }));

        int actualDeleted = db.Table<Book>()
            .Where(b => b.AuthorId == 1)
            .OrderBy(b => b.Price)
            .ExecuteDelete();
        int actualRemaining = db.Table<Book>().Count();

        Assert.Equal(oracleDeleted, actualDeleted);
        Assert.Equal(oracleRemaining, actualRemaining);
    }
}