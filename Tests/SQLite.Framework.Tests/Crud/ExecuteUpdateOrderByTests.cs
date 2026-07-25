using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ExecuteUpdateOrderByTests
{
    [Fact]
    public void ExecuteUpdateWithOrderByUpdatesMatchingRows()
    {
        Book[] seed =
        {
            new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "c", AuthorId = 2, Price = 3 },
        };

        List<Book> memory = seed.Select(b => new Book
        {
            Id = b.Id,
            Title = b.Title,
            AuthorId = b.AuthorId,
            Price = b.Price,
        }).OrderBy(b => b.Id).ToList();
        foreach (Book b in memory.Where(b => b.AuthorId == 1))
        {
            b.Title = "updated";
        }
        int oracleUpdated = memory.Count(b => b.AuthorId == 1);
        int oracleWithNewTitle = memory.Count(b => b.Title == "updated");

        Assert.Equal(2, oracleUpdated);
        Assert.Equal(2, oracleWithNewTitle);

        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(seed.Select(b => new Book
        {
            Id = b.Id,
            Title = b.Title,
            AuthorId = b.AuthorId,
            Price = b.Price,
        }));

        int actualUpdated = db.Table<Book>()
            .Where(b => b.AuthorId == 1)
            .OrderBy(b => b.Id)
            .ExecuteUpdate(s => s.Set(b => b.Title, "updated"));
        int actualWithNewTitle = db.Table<Book>().Count(b => b.Title == "updated");

        Assert.Equal(oracleUpdated, actualUpdated);
        Assert.Equal(oracleWithNewTitle, actualWithNewTitle);
    }
}
