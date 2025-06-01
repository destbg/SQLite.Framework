using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Tests.DTObjects;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ResultTests
{
    public class NullableEntity
    {
        [Key]
        public int? Id { get; set; }

        public string? Name { get; set; }
        public int? Test { get; set; }
        public DateTime? BirthDate { get; set; }
    }

    [Fact]
    public void CallExternalMethodNullJoin()
    {
        using TestDatabase db = SetupDatabase();

        BookDTO book = (
            from b in db.Table<Book>()
            join author in db.Table<Author>() on b.AuthorId equals author.Id - 1 into authorGroup
            from author in authorGroup.DefaultIfEmpty()
            where b.Id == 1
            select new BookDTO
            {
                Id = b.Id,
                Title = b.Title,
                Author = author != null
                    ? new AuthorDTO
                    {
                        Id = author.Id,
                        Name = author.Name,
                        Email = author.Email,
                        BirthDate = author.BirthDate
                    }
                    : null
            }
        ).First();

        Assert.NotNull(book);
        Assert.Equal(1, book.Id);
        Assert.Equal("Book 1", book.Title);
        Assert.Null(book.Author);
    }

    [Fact]
    public void CallExternalMethodNonNullJoin()
    {
        using TestDatabase db = SetupDatabase();

        BookDTO book = (
            from b in db.Table<Book>()
            join author in db.Table<Author>() on b.AuthorId equals author.Id into authorGroup
            from author in authorGroup.DefaultIfEmpty()
            where b.Id == 1
            select new BookDTO
            {
                Id = b.Id,
                Title = b.Title,
                Author = author != null
                    ? new AuthorDTO
                    {
                        Id = author.Id,
                        Name = author.Name,
                        Email = author.Email,
                        BirthDate = author.BirthDate
                    }
                    : null
            }
        ).First();

        Assert.NotNull(book);
        Assert.Equal(1, book.Id);
        Assert.Equal("Book 1", book.Title);
        Assert.NotNull(book.Author);
        Assert.Equal(1, book.Author.Id);
        Assert.Equal("Author 1", book.Author.Name);
    }

    [Fact]
    public void SelectWithAnonymousTypeResult()
    {
        using TestDatabase db = SetupDatabase();

        var list = db.Table<Book>().Select(f => new { f.Id, f.Title }).ToList();

        Assert.NotNull(list);
        Assert.NotEmpty(list);
        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("Book 1", list[0].Title);
        Assert.Equal(2, list[1].Id);
        Assert.Equal("Book 2", list[1].Title);
    }

    [Fact]
    public void ScalarSelectWithResult()
    {
        using TestDatabase db = SetupDatabase();

        List<int> list = db.Table<Book>().Select(f => f.Id).ToList();

        Assert.NotNull(list);
        Assert.NotEmpty(list);
        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
    }

    [Fact]
    public void DeepSelectWithResult()
    {
        using TestDatabase db = SetupDatabase();

        List<BookDTO> list = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            select new BookDTO
            {
                Id = book.Id,
                Title = book.Title,
                Author = new AuthorDTO
                {
                    Id = author.Id,
                    Email = author.Email,
                    Name = author.Name,
                    BirthDate = author.BirthDate
                }
            }
        ).ToList();

        Assert.NotNull(list);
        Assert.NotEmpty(list);
        Assert.Equal(2, list.Count);

        Assert.Equal(1, list[0].Id);
        Assert.Equal("Book 1", list[0].Title);
        Assert.Equal(1, list[0].Author.Id);
        Assert.Equal("Author 1", list[0].Author.Name);
        Assert.Equal("author@mail.com", list[0].Author.Email);
        Assert.Equal(2000, list[0].Author.BirthDate.Year);

        Assert.Equal(2, list[1].Id);
        Assert.Equal("Book 2", list[1].Title);
        Assert.Equal(1, list[1].Author.Id);
        Assert.Equal("Author 1", list[1].Author.Name);
        Assert.Equal("author@mail.com", list[1].Author.Email);
    }

    [Fact]
    public void FirstWithResult()
    {
        using TestDatabase db = SetupDatabase();

        Book first = db.Table<Book>().First(f => f.Id == 1);

        Assert.NotNull(first);
        Assert.Equal(1, first.Id);
        Assert.Equal("Book 1", first.Title);
        Assert.Equal(1, first.AuthorId);
        Assert.Equal(5, first.Price);
    }

    [Fact]
    public void FirstWithoutResult()
    {
        using TestDatabase db = SetupDatabase();

        try
        {
            _ = db.Table<Book>().First(f => f.Id == -1);
            Assert.Fail("Expected exception not thrown.");
        }
        catch (InvalidOperationException)
        {
            // Success
        }
    }

    [Fact]
    public void FirstOrDefaultWithResult()
    {
        using TestDatabase db = SetupDatabase();

        Book? first = db.Table<Book>().FirstOrDefault(f => f.Id == 1);

        Assert.NotNull(first);
        Assert.Equal(1, first.Id);
        Assert.Equal("Book 1", first.Title);
        Assert.Equal(1, first.AuthorId);
        Assert.Equal(5, first.Price);
    }

    [Fact]
    public void FirstOrDefaultWithoutResult()
    {
        using TestDatabase db = SetupDatabase();

        Book? first = db.Table<Book>().FirstOrDefault(f => f.Id == -1);

        Assert.Null(first);
    }

    [Fact]
    public void SingleWithResult()
    {
        using TestDatabase db = SetupDatabase();

        Book single = db.Table<Book>().Single(f => f.Id == 1);

        Assert.NotNull(single);
        Assert.Equal(1, single.Id);
        Assert.Equal("Book 1", single.Title);
        Assert.Equal(1, single.AuthorId);
        Assert.Equal(5, single.Price);
    }

    [Fact]
    public void SingleWithoutResult()
    {
        using TestDatabase db = SetupDatabase();

        try
        {
            _ = db.Table<Book>().Single(f => f.Id == -1);
            Assert.Fail("Expected exception not thrown.");
        }
        catch (InvalidOperationException)
        {
            // Success
        }
    }

    [Fact]
    public void SingleWithMultipleResults()
    {
        using TestDatabase db = SetupDatabase();

        try
        {
            _ = db.Table<Book>().Single(f => f.AuthorId == 1);
            Assert.Fail("Expected exception not thrown.");
        }
        catch (InvalidOperationException)
        {
            // Success
        }
    }

    [Fact]
    public void SingleOrDefaultWithResult()
    {
        using TestDatabase db = SetupDatabase();

        Book? single = db.Table<Book>().SingleOrDefault(f => f.Id == 1);

        Assert.NotNull(single);
        Assert.Equal(1, single.Id);
        Assert.Equal("Book 1", single.Title);
        Assert.Equal(1, single.AuthorId);
        Assert.Equal(5, single.Price);
    }

    [Fact]
    public void SingleOrDefaultWithoutResult()
    {
        using TestDatabase db = SetupDatabase();

        Book? single = db.Table<Book>().SingleOrDefault(f => f.Id == -1);

        Assert.Null(single);
    }

    [Fact]
    public void AnyTrue()
    {
        using TestDatabase db = SetupDatabase();

        bool any = db.Table<Book>().Any(f => f.Id == 1);

        Assert.True(any);
    }

    [Fact]
    public void AnyFalse()
    {
        using TestDatabase db = SetupDatabase();

        bool any = db.Table<Book>().Any(f => f.Id == -1);

        Assert.False(any);
    }

    [Fact]
    public void AllTrue()
    {
        using TestDatabase db = SetupDatabase();

        bool all = db.Table<Book>().All(f => f.Title != "Book -1");

        Assert.True(all);
    }

    [Fact]
    public void AllFalse()
    {
        using TestDatabase db = SetupDatabase();

        bool all = db.Table<Book>().All(f => f.Id != 1);

        Assert.False(all);
    }

    [Fact]
    public void Count()
    {
        using TestDatabase db = SetupDatabase();

        int count = db.Table<Book>().Count(f => f.AuthorId == 1);

        Assert.Equal(2, count);
    }

    [Fact]
    public void Sum()
    {
        using TestDatabase db = SetupDatabase();

        double sum = db.Table<Book>().Sum(f => f.Price);

        Assert.Equal(15, sum);
    }

    [Fact]
    public void Max()
    {
        using TestDatabase db = SetupDatabase();

        double max = db.Table<Book>().Max(f => f.Price);

        Assert.Equal(10, max);
    }

    [Fact]
    public void Min()
    {
        using TestDatabase db = SetupDatabase();

        double min = db.Table<Book>().Min(f => f.Price);

        Assert.Equal(5, min);
    }

    [Fact]
    public void Average()
    {
        using TestDatabase db = SetupDatabase();

        double average = db.Table<Book>().Average(f => f.Price);

        Assert.Equal(7.5, average);
    }

    [Fact]
    public void ContainsTrue()
    {
        using TestDatabase db = SetupDatabase();

        bool contains = db.Table<Book>().Select(f => f.Id).Contains(1);

        Assert.True(contains);
    }

    [Fact]
    public void ContainsFalse()
    {
        using TestDatabase db = SetupDatabase();

        bool contains = db.Table<Book>().Select(f => f.Id).Contains(-1);

        Assert.False(contains);
    }

    [Fact]
    public void WhereContainsTrue()
    {
        using TestDatabase db = SetupDatabase();

        bool contains = db.Table<Book>().Where(f => f.Title.StartsWith("Book%")).Select(f => f.Id).Any();

        Assert.False(contains);
    }

    [Fact]
    public void ResultGroupBySumTable()
    {
        using TestDatabase db = SetupDatabase();

        List<double> sum = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Sum(f => f.Price)
        ).ToList();

        Assert.Single(sum);
        Assert.Equal(15, sum[0]);
    }

    [Fact]
    public void ResultGroupBySimpleSumTable()
    {
        using TestDatabase db = SetupDatabase();

        List<double> sum = (
            from book in db.Table<Book>()
            group book.Price by book.AuthorId
            into g
            select g.Sum()
        ).ToList();

        Assert.Single(sum);
        Assert.Equal(15, sum[0]);
    }

    [Fact]
    public void ResultGroupByComplexSumTable()
    {
        using TestDatabase db = SetupDatabase();

        List<double> sum = (
            from book in db.Table<Book>()
            group new { book.Price, book.AuthorId } by book.AuthorId
            into g
            select g.Sum(f => f.Price)
        ).ToList();

        Assert.Single(sum);
        Assert.Equal(15, sum[0]);
    }

    [Fact]
    public void ResultGroupByAverageTable()
    {
        using TestDatabase db = SetupDatabase();

        List<double> average = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Average(f => f.Price)
        ).ToList();

        Assert.Single(average);
        Assert.Equal(7.5, average[0]);
    }

    [Fact]
    public void ResultGroupByMinTable()
    {
        using TestDatabase db = SetupDatabase();

        List<double> min = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Min(f => f.Price)
        ).ToList();

        Assert.Single(min);
        Assert.Equal(5, min[0]);
    }

    [Fact]
    public void ResultGroupByMaxTable()
    {
        using TestDatabase db = SetupDatabase();

        List<double> max = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Max(f => f.Price)
        ).ToList();

        Assert.Single(max);
        Assert.Equal(10, max[0]);
    }

    [Fact]
    public void ResultGroupByCountTable()
    {
        using TestDatabase db = SetupDatabase();

        List<int> count = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.Count()
        ).ToList();

        Assert.Single(count);
        Assert.Equal(2, count[0]);
    }

    [Fact]
    public void ResultGroupByLongCountTable()
    {
        using TestDatabase db = SetupDatabase();

        List<long> count = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            select g.LongCount()
        ).ToList();

        Assert.Single(count);
        Assert.Equal(2, count[0]);
    }

    [Fact]
    public void CheckNullableProperty()
    {
        using TestDatabase db = new();

        db.Table<NullableEntity>().CreateTable();

        db.Table<NullableEntity>().Add(new NullableEntity
        {
            Id = 1,
            Name = "Test",
            Test = null,
            BirthDate = new DateTime(2000, 1, 1),
        });

        NullableEntity? entity = db.Table<NullableEntity>()
            .Where(f => f.BirthDate.HasValue && !f.Test.HasValue)
            .FirstOrDefault(f => f.Id == 1);

        Assert.NotNull(entity);
        Assert.Equal(1, entity.Id);
        Assert.Equal("Test", entity.Name);
        Assert.Equal(new DateTime(2000, 1, 1), entity.BirthDate);
    }

    [Fact]
    public void ResultComplexWhere()
    {
        using TestDatabase db = SetupDatabase();

        List<Book> books = (
            from book in db.Table<Book>()
            where book.Id == 1
                  && !(book.Id != 3)
                  && (book.Id == 18 || book.AuthorId == 19)
                  && book.Title == null
                  && book.Title != "Test"
                  && (book.Title != null ? 20 : 21) == 22
                  && (book.Title ?? "") == "Book"
            select book
        ).ToList();

        Assert.Empty(books);
    }

    private static TestDatabase SetupDatabase([CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);

        db.Table<Book>().CreateTable();
        db.Table<Author>().CreateTable();

        db.Table<Book>().AddRange(new[]
        {
            new Book
            {
                Id = 1,
                Title = "Book 1",
                AuthorId = 1,
                Price = 5
            },
            new Book
            {
                Id = 2,
                Title = "Book 2",
                AuthorId = 1,
                Price = 10
            }
        });

        db.Table<Author>().AddRange(new[]
        {
            new Author
            {
                Id = 1,
                Name = "Author 1",
                Email = "author@mail.com",
                BirthDate = new DateTime(2000, 1, 1)
            }
        });

        return db;
    }
}