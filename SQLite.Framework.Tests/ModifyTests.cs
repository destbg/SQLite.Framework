using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ModifyTests
{
    [Fact]
    public void Add()
    {
        using TestDatabase db = new();

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

        List<Book> list = db.Table<Book>().ToList();

        Assert.NotNull(list);
        Assert.Equal(2, list.Count);

        Assert.NotNull(list[0]);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("Book 1", list[0].Title);
        Assert.Equal(1, list[0].AuthorId);
        Assert.Equal(5, list[0].Price);

        Assert.NotNull(list[1]);
        Assert.Equal(2, list[1].Id);
        Assert.Equal("Book 2", list[1].Title);
        Assert.Equal(1, list[1].AuthorId);
        Assert.Equal(10, list[1].Price);
    }

    [Fact]
    public void AddNonTransaction()
    {
        using TestDatabase db = new();

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
        }, runInTransaction: false);

        List<Book> list = db.Table<Book>().ToList();

        Assert.NotNull(list);
        Assert.Equal(2, list.Count);

        Assert.NotNull(list[0]);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("Book 1", list[0].Title);
        Assert.Equal(1, list[0].AuthorId);
        Assert.Equal(5, list[0].Price);

        Assert.NotNull(list[1]);
        Assert.Equal(2, list[1].Id);
        Assert.Equal("Book 2", list[1].Title);
        Assert.Equal(1, list[1].AuthorId);
        Assert.Equal(10, list[1].Price);
    }

    [Fact]
    public void Update()
    {
        using TestDatabase db = new();

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
        db.Table<Book>().UpdateRange(new[]
        {
            new Book
            {
                Id = 1,
                Title = "Book 1 Updated",
                AuthorId = 1,
                Price = 6
            }
        });

        List<Book> list = db.Table<Book>().ToList();

        Assert.NotNull(list);
        Assert.Equal(2, list.Count);

        Assert.NotNull(list[0]);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("Book 1 Updated", list[0].Title);
        Assert.Equal(1, list[0].AuthorId);
        Assert.Equal(6, list[0].Price);

        Assert.NotNull(list[1]);
        Assert.Equal(2, list[1].Id);
        Assert.Equal("Book 2", list[1].Title);
        Assert.Equal(1, list[1].AuthorId);
        Assert.Equal(10, list[1].Price);
    }

    [Fact]
    public void UpdateNonTransaction()
    {
        using TestDatabase db = new();

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
        db.Table<Book>().UpdateRange(new[]
        {
            new Book
            {
                Id = 1,
                Title = "Book 1 Updated",
                AuthorId = 1,
                Price = 6
            }
        }, runInTransaction: false);

        List<Book> list = db.Table<Book>().ToList();

        Assert.NotNull(list);
        Assert.Equal(2, list.Count);

        Assert.NotNull(list[0]);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("Book 1 Updated", list[0].Title);
        Assert.Equal(1, list[0].AuthorId);
        Assert.Equal(6, list[0].Price);

        Assert.NotNull(list[1]);
        Assert.Equal(2, list[1].Id);
        Assert.Equal("Book 2", list[1].Title);
        Assert.Equal(1, list[1].AuthorId);
        Assert.Equal(10, list[1].Price);
    }

    [Fact]
    public void Remove()
    {
        using TestDatabase db = new();

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
        db.Table<Book>().RemoveRange(new[]
        {
            new Book
            {
                Id = 1,
                Title = "Book 1",
                AuthorId = 1,
                Price = 5
            }
        });

        List<Book> list = db.Table<Book>().ToList();

        Assert.NotNull(list);
        Assert.Single(list);

        Assert.NotNull(list[0]);
        Assert.Equal(2, list[0].Id);
        Assert.Equal("Book 2", list[0].Title);
        Assert.Equal(1, list[0].AuthorId);
        Assert.Equal(10, list[0].Price);
    }

    [Fact]
    public void RemoveNonTransaction()
    {
        using TestDatabase db = new();

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
        db.Table<Book>().RemoveRange(new[]
        {
            new Book
            {
                Id = 1,
                Title = "Book 1",
                AuthorId = 1,
                Price = 5
            }
        }, runInTransaction: false);

        List<Book> list = db.Table<Book>().ToList();

        Assert.NotNull(list);
        Assert.Single(list);

        Assert.NotNull(list[0]);
        Assert.Equal(2, list[0].Id);
        Assert.Equal("Book 2", list[0].Title);
        Assert.Equal(1, list[0].AuthorId);
        Assert.Equal(10, list[0].Price);
    }

    [Fact]
    public void RemoveSingle()
    {
        using TestDatabase db = new();

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
        db.Table<Book>().Remove(new Book
        {
            Id = 1,
            Title = "Book 1",
            AuthorId = 1,
            Price = 5
        });

        List<Book> list = db.Table<Book>().ToList();

        Assert.NotNull(list);
        Assert.Single(list);

        Assert.NotNull(list[0]);
        Assert.Equal(2, list[0].Id);
        Assert.Equal("Book 2", list[0].Title);
        Assert.Equal(1, list[0].AuthorId);
        Assert.Equal(10, list[0].Price);
    }

    [Fact]
    public void Clear()
    {
        using TestDatabase db = new();

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
        db.Table<Book>().Clear();

        List<Book> list = db.Table<Book>().ToList();

        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public void DropTable()
    {
        using TestDatabase db = new();

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
        db.Table<Book>().DropTable();

        try
        {
            _ = db.Table<Book>().ToList();
            Assert.Fail("Expected exception not thrown.");
        }
        catch (SQLiteException ex)
        {
            Assert.Equal(SQLiteResult.Error, ex.Result);
            // Success
        }
    }
}