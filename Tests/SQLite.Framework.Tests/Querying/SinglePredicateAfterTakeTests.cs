using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SinglePredicateAfterTakeTests
{
    private static Book[] Seed()
    {
        Book[] books = new Book[6];
        for (int i = 1; i <= 6; i++)
        {
            books[i - 1] = new Book { Id = i, Title = "T" + i, AuthorId = 1, Price = i };
        }
        return books;
    }

    private static TestDatabase Create(Book[] seed)
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        foreach (Book b in seed)
        {
            db.Table<Book>().Add(b);
        }
        return db;
    }

    [Fact]
    public void SinglePredicateMatchingInsideWindow_MatchesLinqToObjects()
    {
        Book[] seed = Seed();
        using TestDatabase db = Create(seed);

        int oracle = seed.OrderBy(b => b.Id).Take(4).Single(b => b.Id == 3).Id;
        int actual = db.Table<Book>().OrderBy(b => b.Id).Take(4).Single(b => b.Id == 3).Id;

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SinglePredicateMatchingOutsideWindow_MatchesLinqToObjects()
    {
        Book[] seed = Seed();
        using TestDatabase db = Create(seed);

        Assert.Throws<InvalidOperationException>(() => seed.OrderBy(b => b.Id).Take(2).Single(b => b.Id == 4));
        Assert.Throws<InvalidOperationException>(() => db.Table<Book>().OrderBy(b => b.Id).Take(2).Single(b => b.Id == 4));
    }
}
