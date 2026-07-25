using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CteSetOperationRhsTests
{
    private static TestDatabase SeedBooks()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        });
        return db;
    }

    [Fact]
    public void Concat_RegularTableLeft_CteRight_ProducesValidSql()
    {
        using TestDatabase db = SeedBooks();

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>().Where(b => b.AuthorId == 1));

        List<Book> results = db.Table<Book>()
            .Where(b => b.AuthorId == 2)
            .Concat(from b in cte select b)
            .OrderBy(b => b.Id)
            .ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal(1, results[0].Id);
        Assert.Equal(2, results[1].Id);
        Assert.Equal(3, results[2].Id);
    }

    [Fact]
    public void Union_RegularTableLeft_CteRight_ProducesValidSql()
    {
        using TestDatabase db = SeedBooks();

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>().Where(b => b.Price > 1));

        List<Book> results = db.Table<Book>()
            .Where(b => b.AuthorId == 1)
            .Union(from b in cte select b)
            .OrderBy(b => b.Id)
            .ToList();

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void Concat_RegularTableLeft_CteRight_WithParameter_ProducesValidSql()
    {
        using TestDatabase db = SeedBooks();

        int authorId = 1;
        SQLiteCte<Book> cte = db.With(() => db.Table<Book>().Where(b => b.AuthorId == authorId));

        SQLiteCommand cmd = db.Table<Book>()
            .Where(b => b.AuthorId == 2)
            .Concat(from b in cte select b)
            .ToSqlCommand();

        Assert.Equal(2, cmd.Parameters.Count);
        string afterUnionAll = cmd.CommandText.Substring(cmd.CommandText.IndexOf("UNION ALL"));
        Assert.DoesNotContain("WITH", afterUnionAll);
    }
}
