using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class LinqMethodTests
{
    [Fact]
    public void SelectMany()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Author>().CreateTable();

        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 }
        });

        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "Author 1",
            Email = "author@test.com",
            BirthDate = DateTime.Now
        });

        var results = db.Table<Author>()
            .SelectMany(_ => db.Table<Book>(), (a, b) => new { a.Name, b.Title })
            .ToList();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Reverse()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 1, Price = 30 }
        });

        List<Book> results = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Reverse()
            .ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal(3, results[0].Id);
        Assert.Equal(2, results[1].Id);
        Assert.Equal(1, results[2].Id);
    }

    [Fact]
    public void DoubleReverse()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 1, Price = 30 }
        });

        List<Book> results = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Reverse()
            .Reverse()
            .ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal(1, results[0].Id);
        Assert.Equal(2, results[1].Id);
        Assert.Equal(3, results[2].Id);
    }

    [Fact]
    public void LastNotSupported()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 1, Price = 30 }
        });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .OrderBy(b => b.Id)
                .Last()
        );
    }

    [Fact]
    public void LastOrDefaultNotSupported()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 }
        });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .OrderBy(b => b.Id)
                .LastOrDefault()
        );
    }

    [Fact]
    public void ElementAtNotSupported()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 1, Price = 30 }
        });

        Assert.Throws<NotSupportedException>(() => db.Table<Book>().ElementAt(1));
    }

    [Fact]
    public void ElementAtOrDefaultNotSupported()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 }
        });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .OrderBy(b => b.Id)
                .ElementAtOrDefault(5)
        );
    }

    [Fact]
    public void DefaultIfEmptyNotSupported()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        Assert.Throws<NotSupportedException>(() => db.Table<Book>().DefaultIfEmpty().ToList());
    }

    [Fact]
    public void SequenceEqualNotSupported()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 }
        });

        IQueryable<int> query1 = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id);
        IQueryable<int> query2 = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id);

        Assert.Throws<NotSupportedException>(() => query1.SequenceEqual(query2));
    }

    [Fact]
    public void Cast()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Book 1",
            AuthorId = 1,
            Price = 10
        });

        List<double> results = db.Table<Book>()
            .Select(b => (object)b.Price)
            .Cast<double>()
            .ToList();

        Assert.Single(results);
        Assert.Equal(10, results[0]);
    }

    [Fact]
    public void OfTypeBoxAndUnboxNotSupported()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 }
        });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Select(b => (object)b)
                .OfType<Book>()
                .ToList()
        );
    }

    [Fact]
    public void Zip()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Author>().CreateTable();

        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 }
        });

        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "Author 1",
            Email = "author@test.com",
            BirthDate = DateTime.Now
        });

        List<Book> books = db.Table<Book>().OrderBy(b => b.Id).ToList();
        List<Author> authors = db.Table<Author>().ToList();

        var results = books.Zip(authors, (b, a) => new { b.Title, a.Name }).ToList();

        Assert.Single(results);
        Assert.Equal("Book 1", results[0].Title);
        Assert.Equal("Author 1", results[0].Name);
    }

    [Fact]
    public void LongCount()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 }
        });

        long count = db.Table<Book>().LongCount();

        Assert.Equal(2L, count);
    }

    [Fact]
    public void LongCountWithPredicate()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 }
        });

        long count = db.Table<Book>().LongCount(b => b.Price > 15);

        Assert.Equal(1L, count);
    }

    [Fact]
    public void Except()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 1, Price = 30 }
        });

        IQueryable<int> allBooks = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> expensiveBooks = db.Table<Book>().Where(b => b.Price > 15).Select(b => b.Id);
        List<int> cheapBooks = allBooks.Except(expensiveBooks).ToList();

        Assert.Single(cheapBooks);
        Assert.Equal(1, cheapBooks[0]);
    }

    [Fact]
    public void Intersect()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 2, Price = 30 }
        });

        IQueryable<int> author1Books = db.Table<Book>().Where(b => b.AuthorId == 1).Select(b => b.Id);
        IQueryable<int> expensiveBooks = db.Table<Book>().Where(b => b.Price > 15).Select(b => b.Id);
        List<int> expensiveAuthor1Books = author1Books.Intersect(expensiveBooks).ToList();

        Assert.Single(expensiveAuthor1Books);
        Assert.Equal(2, expensiveAuthor1Books[0]);
    }

    [Fact]
    public void GroupJoinCountNotSupported()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Author>().CreateTable();

        db.Table<Author>().AddRange(new[]
        {
            new Author { Id = 1, Name = "Author 1", Email = "author1@test.com", BirthDate = DateTime.Now },
            new Author { Id = 2, Name = "Author 2", Email = "author2@test.com", BirthDate = DateTime.Now }
        });

        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 }
        });

        Assert.Throws<ArgumentException>(() => (
            from author in db.Table<Author>()
            join book in db.Table<Book>() on author.Id equals book.AuthorId into books
            select new
            {
                Author = author.Name,
                BookCount = books.Count()
            }
        ).ToList());
    }

    [Fact]
    public void ComplexNestedQuery()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Author>().CreateTable();

        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "Author 1",
            Email = "author@test.com",
            BirthDate = DateTime.Now
        });

        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 1, Price = 30 }
        });

        double avgPrice = db.Table<Book>().Average(b => b.Price);

        List<Book> results = db.Table<Book>()
            .Where(b => b.Price >= avgPrice)
            .OrderByDescending(b => b.Price)
            .ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal(30, results[0].Price);
        Assert.Equal(20, results[1].Price);
    }

    [Fact]
    public void MultipleSelectProjections()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test Book",
            AuthorId = 1,
            Price = 10.50
        });

        var result = db.Table<Book>()
            .Select(b => new { b.Id, b.Title, b.Price })
            .Select(x => new { x.Id, x.Title, DiscountedPrice = x.Price * 0.9 })
            .First();

        Assert.Equal(1, result.Id);
        Assert.Equal("Test Book", result.Title);
        Assert.Equal(9.45, result.DiscountedPrice, 2);
    }

    [Fact]
    public void WhereWithComplexCondition()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book A", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book B", AuthorId = 2, Price = 20 },
            new Book { Id = 3, Title = "Book C", AuthorId = 1, Price = 30 }
        });

        List<Book> results = db.Table<Book>()
            .Where(b => (b.AuthorId == 1 && b.Price > 15) || (b.AuthorId == 2 && b.Price < 25))
            .ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, b => b.Id == 2);
        Assert.Contains(results, b => b.Id == 3);
    }

    [Fact]
    public void TakeWhile()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 1, Price = 30 }
        });

        List<Book> results = db.Table<Book>()
            .OrderBy(b => b.Id)
            .ToList()
            .TakeWhile(b => b.Price < 25)
            .ToList();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void SkipWhile()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 1, Price = 30 }
        });

        List<Book> results = db.Table<Book>()
            .OrderBy(b => b.Id)
            .ToList()
            .SkipWhile(b => b.Price < 25)
            .ToList();

        Assert.Single(results);
        Assert.Equal(3, results[0].Id);
    }
}