using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FirstPredicateAfterTakeTests
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
    public void FirstOrDefaultPredicateMatchingOutsideWindow_MatchesLinqToObjects()
    {
        Book[] seed = Seed();
        using TestDatabase db = Create(seed);

        Book? oracle = seed.OrderBy(b => b.Id).Take(3).FirstOrDefault(b => b.Id == 5);
        Book? actual = db.Table<Book>().OrderBy(b => b.Id).Take(3).FirstOrDefault(b => b.Id == 5);

        Assert.Null(oracle);
        Assert.Null(actual);
    }

    [Fact]
    public void FirstOrDefaultPredicateMatchingInsideWindow_MatchesLinqToObjects()
    {
        Book[] seed = Seed();
        using TestDatabase db = Create(seed);

        Book? oracle = seed.OrderBy(b => b.Id).Take(3).FirstOrDefault(b => b.Id == 2);
        Book? actual = db.Table<Book>().OrderBy(b => b.Id).Take(3).FirstOrDefault(b => b.Id == 2);

        Assert.Equal(oracle!.Id, actual!.Id);
    }
}
