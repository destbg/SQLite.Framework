using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SetOperationGroupingTests
{
    [Fact]
    public void ConcatThenGroupByCountMatchesObjects()
    {
        Book[] rows = Rows();
        using TestDatabase db = Seed(rows);

        int expected = rows.Where(book => book.Id <= 3).Select(book => book.AuthorId)
            .Concat(rows.Where(book => book.Id >= 2).Select(book => book.AuthorId))
            .GroupBy(authorId => authorId)
            .Count();
        int actual = db.Table<Book>().Where(book => book.Id <= 3).Select(book => book.AuthorId)
            .Concat(db.Table<Book>().Where(book => book.Id >= 2).Select(book => book.AuthorId))
            .GroupBy(authorId => authorId)
            .Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EmptySetThenGroupByCountsMatchObjects()
    {
        Book[] rows = Rows();
        using TestDatabase db = Seed(rows);

        IEnumerable<IGrouping<int, int>> expectedGroups = rows.Where(book => book.Id < 0).Select(book => book.AuthorId)
            .Concat(rows.Where(book => book.Id < 0).Select(book => book.AuthorId))
            .GroupBy(authorId => authorId);
        IQueryable<IGrouping<int, int>> actualGroups = db.Table<Book>().Where(book => book.Id < 0).Select(book => book.AuthorId)
            .Concat(db.Table<Book>().Where(book => book.Id < 0).Select(book => book.AuthorId))
            .GroupBy(authorId => authorId);

        Assert.Equal(expectedGroups.Count(), actualGroups.Count());
        Assert.Equal(expectedGroups.LongCount(), actualGroups.LongCount());
    }

    [Fact]
    public void GroupedCountsThenSumAndAverageMatchObjects()
    {
        Book[] rows = Rows();
        using TestDatabase db = Seed(rows);

        IEnumerable<int> expectedCounts = rows.Where(book => book.Id <= 3).Select(book => book.AuthorId)
            .Concat(rows.Where(book => book.Id >= 2).Select(book => book.AuthorId))
            .GroupBy(authorId => authorId)
            .Select(group => group.Count());
        IQueryable<int> actualCounts = db.Table<Book>().Where(book => book.Id <= 3).Select(book => book.AuthorId)
            .Concat(db.Table<Book>().Where(book => book.Id >= 2).Select(book => book.AuthorId))
            .GroupBy(authorId => authorId)
            .Select(group => group.Count());

        Assert.Equal(expectedCounts.Sum(), actualCounts.Sum());
        Assert.Equal(expectedCounts.Average(), actualCounts.Average());
    }

    [Fact]
    public void UnionThenGroupByLongCountMatchesObjects()
    {
        Book[] rows = Rows();
        using TestDatabase db = Seed(rows);

        long expected = rows.Where(book => book.Id <= 3).Select(book => book.AuthorId)
            .Union(rows.Where(book => book.Id >= 2).Select(book => book.AuthorId))
            .GroupBy(authorId => authorId)
            .LongCount();
        long actual = db.Table<Book>().Where(book => book.Id <= 3).Select(book => book.AuthorId)
            .Union(db.Table<Book>().Where(book => book.Id >= 2).Select(book => book.AuthorId))
            .GroupBy(authorId => authorId)
            .LongCount();

        Assert.Equal(expected, actual);
    }

    private static Book[] Rows()
    {
        return
        [
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = -1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 0 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 1 },
            new Book { Id = 4, Title = "D", AuthorId = 3, Price = 2 }
        ];
    }

    private static TestDatabase Seed(Book[] rows)
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(rows);
        return db;
    }
}
