using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupByCountPredicateTests
{
    private static TestDatabase SeedBooks()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 2, Price = 4 },
            new Book { Id = 5, Title = "E", AuthorId = 2, Price = 5 },
        });
        return db;
    }

    [Fact]
    public void GroupBy_CountWithKeyPredicate_ReturnsGroupCount()
    {
        using TestDatabase db = SeedBooks();

        int oracle = new[] { 1, 1, 1, 2, 2 }
            .GroupBy(x => x)
            .Count(g => g.Key == 1);

        int actual = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .Count(g => g.Key == 1);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupBy_CountWithKeyPredicate_NoMatchingGroup_ReturnsZero()
    {
        using TestDatabase db = SeedBooks();

        int oracle = new[] { 1, 1, 1, 2, 2 }
            .GroupBy(x => x)
            .Count(g => g.Key == 99);

        int actual = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .Count(g => g.Key == 99);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupBy_LongCountWithKeyPredicate_ReturnsGroupCount()
    {
        using TestDatabase db = SeedBooks();

        long oracle = new[] { 1, 1, 1, 2, 2 }
            .GroupBy(x => x)
            .LongCount(g => g.Key == 2);

        long actual = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .LongCount(g => g.Key == 2);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupBy_CountAllGroups_NoPredicateStillWorks()
    {
        using TestDatabase db = SeedBooks();

        int oracle = new[] { 1, 1, 1, 2, 2 }
            .GroupBy(x => x)
            .Count();

        int actual = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .Count();

        Assert.Equal(oracle, actual);
    }
}
