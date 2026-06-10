using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class QuerySyntaxGroupByAnonymousKeyTests
{
    private static TestDatabase Seed()
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

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void QuerySyntax_GroupByAnonymousKey_MaterializesKeys()
    {
        using TestDatabase db = Seed();

        var oracle = new[] { (Id: 1, AuthorId: 1), (Id: 2, AuthorId: 1), (Id: 3, AuthorId: 2) }
            .GroupBy(b => new { b.AuthorId })
            .Select(g => g.Key.AuthorId)
            .OrderBy(x => x)
            .ToList();

        var groups = (
            from b in db.Table<Book>()
            group b by new { b.AuthorId }
        ).ToList();

        List<int> actual = groups.Select(g => g.Key.AuthorId).OrderBy(x => x).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Fluent_GroupByAnonymousKey_MaterializesKeys()
    {
        using TestDatabase db = Seed();

        var oracle = new[] { (Id: 1, AuthorId: 1), (Id: 2, AuthorId: 1), (Id: 3, AuthorId: 2) }
            .GroupBy(b => new { b.AuthorId })
            .Select(g => g.Key.AuthorId)
            .OrderBy(x => x)
            .ToList();

        var groups = db.Table<Book>()
            .GroupBy(b => new { b.AuthorId })
            .ToList();

        List<int> actual = groups.Select(g => g.Key.AuthorId).OrderBy(x => x).ToList();

        Assert.Equal(oracle, actual);
    }
#endif
}
