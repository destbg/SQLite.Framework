using SQLite.Framework.Tests.DTObjects;
using SQLite.Framework.Tests.Entities;

namespace SQLite.Framework.Tests;

public class ResultTests
{
    [Fact]
    public void SelectWithAnonymousTypeResult()
    {
        using SQLiteDatabase db = SetupDatabase();

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
        using SQLiteDatabase db = SetupDatabase();

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
        using SQLiteDatabase db = SetupDatabase();

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
        using SQLiteDatabase db = SetupDatabase();

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
        using SQLiteDatabase db = SetupDatabase();

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
        using SQLiteDatabase db = SetupDatabase();

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
        using SQLiteDatabase db = SetupDatabase();

        Book? first = db.Table<Book>().FirstOrDefault(f => f.Id == -1);

        Assert.Null(first);
    }

    [Fact]
    public void SingleWithResult()
    {
        using SQLiteDatabase db = SetupDatabase();

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
        using SQLiteDatabase db = SetupDatabase();

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
        using SQLiteDatabase db = SetupDatabase();

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
        using SQLiteDatabase db = SetupDatabase();

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
        using SQLiteDatabase db = SetupDatabase();

        Book? single = db.Table<Book>().SingleOrDefault(f => f.Id == -1);

        Assert.Null(single);
    }

    [Fact]
    public void AnyTrue()
    {
        using SQLiteDatabase db = SetupDatabase();

        bool any = db.Table<Book>().Any(f => f.Id == 1);

        Assert.True(any);
    }

    [Fact]
    public void AnyFalse()
    {
        using SQLiteDatabase db = SetupDatabase();

        bool any = db.Table<Book>().Any(f => f.Id == -1);

        Assert.False(any);
    }

    [Fact]
    public void AllTrue()
    {
        using SQLiteDatabase db = SetupDatabase();

        bool all = db.Table<Book>().All(f => f.Title != "Book -1");

        Assert.True(all);
    }

    [Fact]
    public void AllFalse()
    {
        using SQLiteDatabase db = SetupDatabase();

        bool all = db.Table<Book>().All(f => f.Id != 1);

        Assert.False(all);
    }

    [Fact]
    public void Count()
    {
        using SQLiteDatabase db = SetupDatabase();

        int count = db.Table<Book>().Count(f => f.AuthorId == 1);

        Assert.Equal(2, count);
    }

    [Fact]
    public void Sum()
    {
        using SQLiteDatabase db = SetupDatabase();

        double sum = db.Table<Book>().Sum(f => f.Price);

        Assert.Equal(15, sum);
    }

    [Fact]
    public void Max()
    {
        using SQLiteDatabase db = SetupDatabase();

        double max = db.Table<Book>().Max(f => f.Price);

        Assert.Equal(10, max);
    }

    [Fact]
    public void Min()
    {
        using SQLiteDatabase db = SetupDatabase();

        double min = db.Table<Book>().Min(f => f.Price);

        Assert.Equal(5, min);
    }

    [Fact]
    public void Average()
    {
        using SQLiteDatabase db = SetupDatabase();

        double average = db.Table<Book>().Average(f => f.Price);

        Assert.Equal(7.5, average);
    }

    [Fact]
    public void ContainsTrue()
    {
        using SQLiteDatabase db = SetupDatabase();

        bool contains = db.Table<Book>().Select(f => f.Id).Contains(1);

        Assert.True(contains);
    }

    [Fact]
    public void ContainsFalse()
    {
        using SQLiteDatabase db = SetupDatabase();

        bool contains = db.Table<Book>().Select(f => f.Id).Contains(-1);

        Assert.False(contains);
    }

    private static SQLiteDatabase SetupDatabase()
    {
        SQLiteDatabase db = new("Data Source=:memory:");
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