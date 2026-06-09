using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SinglePredicateAfterTakeTests
{
    [Fact]
    public void SinglePredicateAfterTake_AppliesPredicateBeforeLimit()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        for (int i = 1; i <= 6; i++)
        {
            db.Table<Book>().Add(new Book { Id = i, Title = "T" + i, AuthorId = 1, Price = i });
        }

        Book actual = db.Table<Book>().OrderBy(b => b.Id).Take(2).Single(b => b.Id == 4);

        Assert.Equal(4, actual.Id);
    }
}
