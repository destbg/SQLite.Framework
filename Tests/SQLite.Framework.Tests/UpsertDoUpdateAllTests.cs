using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UpsertDoUpdateAllTests
{
    [Fact]
    public void DoUpdateAllDoesNotRewritePrimaryKeyOnNonKeyConflict()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 10, Title = "A", AuthorId = 1, Price = 5.0 });

        db.Table<Book>().Upsert(
            new Book { Id = 20, Title = "B", AuthorId = 2, Price = 5.0 },
            c => c.OnConflict(b => b.Price).DoUpdateAll());

        Book row = db.Table<Book>().Single();
        Assert.Equal("B", row.Title);
        Assert.Equal(10, row.Id);
    }
}
