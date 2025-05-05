using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.DTObjects;
using SQLite.Framework.Tests.Entities;

namespace SQLite.Framework.Tests;

public class AsyncTests
{
    [Fact]
    public async Task ScalarSelectWithResult()
    {
        using SQLiteDatabase db = SetupDatabase();

        List<int> list = await db.Table<Book>().Select(f => f.Id).ToListAsync();

        Assert.NotNull(list);
        Assert.NotEmpty(list);
        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
    }

    [Fact]
    public async Task DeepSelectWithResult()
    {
        using SQLiteDatabase db = SetupDatabase();

        List<BookDTO> list = await (
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
        ).ToListAsync();

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
        Assert.Equal(2000, list[1].Author.BirthDate.Year);
    }

    [Fact]
    public async Task FirstWithResult()
    {
        using SQLiteDatabase db = SetupDatabase();

        Book first = await db.Table<Book>().FirstAsync(f => f.Id == 1);

        Assert.NotNull(first);
        Assert.Equal(1, first.Id);
        Assert.Equal("Book 1", first.Title);
        Assert.Equal(1, first.AuthorId);
        Assert.Equal(5, first.Price);
    }

    [Fact]
    public async Task FirstWithoutResult()
    {
        using SQLiteDatabase db = SetupDatabase();

        try
        {
            _ = await db.Table<Book>().FirstAsync(f => f.Id == -1);
            Assert.Fail("Expected exception not thrown.");
        }
        catch (InvalidOperationException)
        {
            // Success
        }
    }

    [Fact]
    public async Task FirstOrDefaultWithResult()
    {
        using SQLiteDatabase db = SetupDatabase();

        Book? first = await db.Table<Book>().FirstOrDefaultAsync(f => f.Id == 1);

        Assert.NotNull(first);
        Assert.Equal(1, first.Id);
        Assert.Equal("Book 1", first.Title);
        Assert.Equal(1, first.AuthorId);
        Assert.Equal(5, first.Price);
    }

    [Fact]
    public async Task FirstOrDefaultWithoutResult()
    {
        using SQLiteDatabase db = SetupDatabase();

        Book? first = await db.Table<Book>().FirstOrDefaultAsync(f => f.Id == -1);

        Assert.Null(first);
    }

    [Fact]
    public async Task SingleWithResult()
    {
        using SQLiteDatabase db = SetupDatabase();

        Book single = await db.Table<Book>().SingleAsync(f => f.Id == 1);

        Assert.NotNull(single);
        Assert.Equal(1, single.Id);
        Assert.Equal("Book 1", single.Title);
        Assert.Equal(1, single.AuthorId);
        Assert.Equal(5, single.Price);
    }

    [Fact]
    public async Task SingleWithoutResult()
    {
        using SQLiteDatabase db = SetupDatabase();

        try
        {
            _ = await db.Table<Book>().SingleAsync(f => f.Id == -1);
            Assert.Fail("Expected exception not thrown.");
        }
        catch (InvalidOperationException)
        {
            // Success
        }
    }

    [Fact]
    public async Task SingleWithMultipleResults()
    {
        using SQLiteDatabase db = SetupDatabase();

        try
        {
            _ = await db.Table<Book>().SingleAsync(f => f.AuthorId == 1);
            Assert.Fail("Expected exception not thrown.");
        }
        catch (InvalidOperationException)
        {
            // Success
        }
    }

    [Fact]
    public async Task SingleOrDefaultWithResult()
    {
        using SQLiteDatabase db = SetupDatabase();

        Book? single = await db.Table<Book>().SingleOrDefaultAsync(f => f.Id == 1);

        Assert.NotNull(single);
        Assert.Equal(1, single.Id);
        Assert.Equal("Book 1", single.Title);
        Assert.Equal(1, single.AuthorId);
        Assert.Equal(5, single.Price);
    }

    [Fact]
    public async Task SingleOrDefaultWithoutResult()
    {
        using SQLiteDatabase db = SetupDatabase();

        Book? single = await db.Table<Book>().SingleOrDefaultAsync(f => f.Id == -1);

        Assert.Null(single);
    }

    [Fact]
    public async Task AnyTrue()
    {
        using SQLiteDatabase db = SetupDatabase();

        bool any = await db.Table<Book>().AnyAsync(f => f.Id == 1);

        Assert.True(any);
    }

    [Fact]
    public async Task AnyFalse()
    {
        using SQLiteDatabase db = SetupDatabase();

        bool any = await db.Table<Book>().AnyAsync(f => f.Id == -1);

        Assert.False(any);
    }

    [Fact]
    public async Task AllTrue()
    {
        using SQLiteDatabase db = SetupDatabase();

        bool all = await db.Table<Book>().AllAsync(f => f.Title != "Book -1");

        Assert.True(all);
    }

    [Fact]
    public async Task AllFalse()
    {
        using SQLiteDatabase db = SetupDatabase();

        bool all = await db.Table<Book>().AllAsync(f => f.Id != 1);

        Assert.False(all);
    }

    [Fact]
    public async Task Count()
    {
        using SQLiteDatabase db = SetupDatabase();

        int count = await db.Table<Book>().CountAsync(f => f.AuthorId == 1);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Sum()
    {
        using SQLiteDatabase db = SetupDatabase();

        double sum = await db.Table<Book>().SumAsync(f => f.Price);

        Assert.Equal(15, sum);
    }

    [Fact]
    public async Task Max()
    {
        using SQLiteDatabase db = SetupDatabase();

        double max = await db.Table<Book>().MaxAsync(f => f.Price);

        Assert.Equal(10, max);
    }

    [Fact]
    public async Task Min()
    {
        using SQLiteDatabase db = SetupDatabase();

        double min = await db.Table<Book>().MinAsync(f => f.Price);

        Assert.Equal(5, min);
    }

    [Fact]
    public async Task Average()
    {
        using SQLiteDatabase db = SetupDatabase();

        double average = await db.Table<Book>().AverageAsync(f => f.Price);

        Assert.Equal(7.5, average);
    }

    [Fact]
    public async Task ContainsTrue()
    {
        using SQLiteDatabase db = SetupDatabase();

        bool contains = await db.Table<Book>().Select(f => f.Id).ContainsAsync(1);

        Assert.True(contains);
    }

    [Fact]
    public async Task ContainsFalse()
    {
        using SQLiteDatabase db = SetupDatabase();

        bool contains = await db.Table<Book>().Select(f => f.Id).ContainsAsync(-1);

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