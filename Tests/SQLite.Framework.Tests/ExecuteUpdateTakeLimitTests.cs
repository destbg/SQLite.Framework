using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ExecuteUpdateTakeLimitTests
{
    [Fact]
    public void ExecuteUpdateWithTakeThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });

        Assert.Throws<NotSupportedException>(() => db.Table<Book>().Take(1).ExecuteUpdate(s => s.Set(b => b.Price, 99.0)));
    }
}
