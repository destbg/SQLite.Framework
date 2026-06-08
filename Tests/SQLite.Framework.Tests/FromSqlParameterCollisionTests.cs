using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FromSqlParameterCollisionTests
{
    [Fact]
    public void FromSqlNamedParameterComposedWithWhereMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Book[] seed =
        [
            new Book { Id = 1, Title = "Alpha", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Beta", AuthorId = 1, Price = 20 },
            new Book { Id = 3, Title = "Gamma", AuthorId = 2, Price = 30 },
        ];
        foreach (Book b in seed)
        {
            db.Table<Book>().Add(b);
        }

        int oracle = seed.Where(b => b.AuthorId == 1).Where(b => b.Price < 25).Count();

        int actual = db.FromSql<Book>(
                "SELECT * FROM \"Books\" WHERE \"BookAuthorId\" = @p1",
                new SQLiteParameter { Name = "@p1", Value = 1 })
            .Where(b => b.Price < 25)
            .ToList()
            .Count;

        Assert.Equal(2, oracle);
        Assert.Equal(oracle, actual);
    }
}
