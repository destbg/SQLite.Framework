using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupJoinCountCompositionTests
{
    [Fact]
    public void CountAfterGroupJoinCountProjectionMatchesObjects()
    {
        (Author[] authors, Book[] books) = Rows();
        using TestDatabase db = Seed(authors, books);

        int expected = authors.GroupJoin(
                books,
                author => author.Id,
                book => book.AuthorId,
                (author, group) => new { author.Id, BookCount = group.Count() })
            .Count();
        int actual = db.Table<Author>().GroupJoin(
                db.Table<Book>(),
                author => author.Id,
                book => book.AuthorId,
                (author, group) => new { author.Id, BookCount = group.Count() })
            .Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DuplicateOuterScalarRowsRemainDistinctOccurrences()
    {
        (Author[] authors, Book[] books) = Rows();
        using TestDatabase db = Seed(authors, books);

        List<int> expected = books.Select(book => book.AuthorId)
            .GroupJoin(
                authors,
                authorId => authorId,
                author => author.Id,
                (authorId, group) => group.Count())
            .OrderBy(count => count)
            .ToList();
        List<int> actual = db.Table<Book>().Select(book => book.AuthorId)
            .GroupJoin(
                db.Table<Author>(),
                authorId => authorId,
                author => author.Id,
                (authorId, group) => group.Count())
            .OrderBy(count => count)
            .ToList();

        Assert.Equal(expected, actual);
        Assert.Equal(4, actual.Count);
    }

    [Fact]
    public void SumAndAverageAfterGroupJoinCountMatchObjects()
    {
        (Author[] authors, Book[] books) = Rows();
        using TestDatabase db = Seed(authors, books);

        IEnumerable<int> expectedCounts = authors.GroupJoin(
            books,
            author => author.Id,
            book => book.AuthorId,
            (author, group) => group.Count());
        IQueryable<int> actualCounts = db.Table<Author>().GroupJoin(
            db.Table<Book>(),
            author => author.Id,
            book => book.AuthorId,
            (author, group) => group.Count());

        Assert.Equal(expectedCounts.Sum(), actualCounts.Sum());
        Assert.Equal(expectedCounts.Average(), actualCounts.Average());
    }

    [Fact]
    public void LongCountAfterGroupJoinMatchesObjects()
    {
        (Author[] authors, Book[] books) = Rows();
        using TestDatabase db = Seed(authors, books);

        List<long> expected = authors.GroupJoin(
                books,
                author => author.Id,
                book => book.AuthorId,
                (author, group) => group.LongCount())
            .OrderBy(count => count)
            .ToList();
        List<long> actual = db.Table<Author>().GroupJoin(
                db.Table<Book>(),
                author => author.Id,
                book => book.AuthorId,
                (author, group) => group.LongCount())
            .OrderBy(count => count)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComputedOuterValueBeforeGroupFlatteningMatchesObjects()
    {
        (Author[] authors, Book[] books) = Rows();
        using TestDatabase db = Seed(authors, books);

        List<(int AuthorId, int BookId)> expected = authors.GroupJoin(
                books,
                author => author.Id,
                book => book.AuthorId,
                (author, group) => new { AuthorId = Math.Abs(author.Id), group })
            .SelectMany(
                value => value.group.DefaultIfEmpty(),
                (value, book) => new ValueTuple<int, int>(value.AuthorId, book == null ? 0 : book.Id))
            .OrderBy(value => value.Item1)
            .ThenBy(value => value.Item2)
            .ToList();
        List<(int AuthorId, int BookId)> actual = db.Table<Author>().GroupJoin(
                db.Table<Book>(),
                author => author.Id,
                book => book.AuthorId,
                (author, group) => new { AuthorId = Math.Abs(author.Id), group })
            .SelectMany(
                value => value.group.DefaultIfEmpty(),
                (value, book) => new ValueTuple<int, int>(value.AuthorId, book == null ? 0 : book.Id))
            .OrderBy(value => value.Item1)
            .ThenBy(value => value.Item2)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountAlongsideTheDirectGroupThrowsNotSupported()
    {
        (Author[] authors, Book[] books) = Rows();
        using TestDatabase db = Seed(authors, books);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<Author>().GroupJoin(
                db.Table<Book>(),
                author => author.Id,
                book => book.AuthorId,
                (author, group) => new { Count = group.Count(), Any = group.Any(), group })
            .ToList());

        Assert.Contains("only supported when followed by", exception.Message);
    }

    private static (Author[] Authors, Book[] Books) Rows()
    {
        Author[] authors =
        [
            new Author { Id = 1, Name = "A", Email = "a@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "B", Email = "b@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 3, Name = "C", Email = "c@x", BirthDate = new DateTime(2000, 1, 1) }
        ];
        Book[] books =
        [
            new Book { Id = 1, Title = "A1", AuthorId = 1, Price = -1 },
            new Book { Id = 2, Title = "A2", AuthorId = 1, Price = 0 },
            new Book { Id = 3, Title = "A3", AuthorId = 1, Price = 1 },
            new Book { Id = 4, Title = "B1", AuthorId = 2, Price = 2 }
        ];

        return (authors, books);
    }

    private static TestDatabase Seed(Author[] authors, Book[] books)
    {
        TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().AddRange(authors);
        db.Table<Book>().AddRange(books);
        return db;
    }
}
