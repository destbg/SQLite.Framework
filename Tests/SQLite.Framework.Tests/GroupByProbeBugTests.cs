using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupByProbeBugTests
{
    [Fact]
    public void GroupCountWithPredicateAppliesThePredicate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 11 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 1, Price = 22 });

        var rows = (from book in db.Table<Book>()
                    group book by book.AuthorId
                    into g
                    select new { g.Key, Expensive = g.Count(b => b.Price >= 10) }).ToList();

        Assert.Single(rows);
        Assert.Equal(2, rows[0].Expensive);
    }

    [Fact]
    public void GroupAverageOverEmptyFilterDoesNotCrash()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 2, Price = 50 });

        List<double> averages = (from book in db.Table<Book>()
                                 group book by book.AuthorId
                                 into g
                                 select g.Where(x => x.Price > 10000).Average(x => x.Price)).ToList();

        Assert.Equal(2, averages.Count);
        Assert.All(averages, a => Assert.Equal(0.0, a));
    }
}
