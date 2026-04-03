using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
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
            db.Table<Book>().ToList();
            Assert.Fail("Expected exception not thrown.");
        }
        catch (SQLiteException ex)
        {
            Assert.Equal(SQLiteResult.Error, ex.Result);
            // Success
        }
    }

    [Fact]
    public void ExecuteDelete()
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

        db.Table<Book>().Where(b => b.Id == 1).ExecuteDelete();

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
    public void ExecuteDeleteSpecific()
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

        db.Table<Book>().ExecuteDelete(b => b.Id == 1);

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
    public void ExecuteUpdate()
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

        db.Table<Book>().ExecuteUpdate(s =>
            s.Set(f => f.Price, f => f.Price + 1)
                .Set(f => f.AuthorId, 2)
        );

        List<Book> list = db.Table<Book>().ToList();

        Assert.NotNull(list);
        Assert.Equal(2, list.Count);
        Assert.NotNull(list[0]);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("Book 1", list[0].Title);
        Assert.Equal(2, list[0].AuthorId);
        Assert.Equal(6, list[0].Price);
        Assert.NotNull(list[1]);
        Assert.Equal(2, list[1].Id);
        Assert.Equal("Book 2", list[1].Title);
        Assert.Equal(2, list[1].AuthorId);
        Assert.Equal(11, list[1].Price);
    }

    [Fact]
    public void ExecuteUpdateSpecific()
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

        db.Table<Book>().Where(f => f.Id == 1).ExecuteUpdate(s =>
            s.Set(f => f.Price, f => f.Price + 1)
                .Set(f => f.AuthorId, 2)
        );

        List<Book> list = db.Table<Book>().ToList();

        Assert.NotNull(list);
        Assert.Equal(2, list.Count);
        Assert.NotNull(list[0]);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("Book 1", list[0].Title);
        Assert.Equal(2, list[0].AuthorId);
        Assert.Equal(6, list[0].Price);
        Assert.NotNull(list[1]);
        Assert.Equal(2, list[1].Id);
        Assert.Equal("Book 2", list[1].Title);
        Assert.Equal(1, list[1].AuthorId);
        Assert.Equal(10, list[1].Price);
    }

    [Fact]
    public void AddOrUpdate_InsertsNewRow()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        db.Table<Book>().AddOrUpdate(new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 5 });

        List<Book> list = db.Table<Book>().ToList();

        Assert.Single(list);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("Book 1", list[0].Title);
        Assert.Equal(5, list[0].Price);
    }

    [Fact]
    public void AddOrUpdate_ReplacesExistingRow()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 5 });
        db.Table<Book>().AddOrUpdate(new Book { Id = 1, Title = "Book 1 Updated", AuthorId = 1, Price = 9 });

        List<Book> list = db.Table<Book>().ToList();

        Assert.Single(list);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("Book 1 Updated", list[0].Title);
        Assert.Equal(9, list[0].Price);
    }

    [Fact]
    public void AddOrUpdateRange_InsertsNewRows()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        db.Table<Book>().AddOrUpdateRange([
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 10 },
        ]);

        List<Book> list = db.Table<Book>().ToList();

        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("Book 1", list[0].Title);
        Assert.Equal(2, list[1].Id);
        Assert.Equal("Book 2", list[1].Title);
    }

    [Fact]
    public void AddOrUpdateRange_ReplacesExistingRows()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 10 },
        ]);

        db.Table<Book>().AddOrUpdateRange([
            new Book { Id = 1, Title = "Book 1 Updated", AuthorId = 1, Price = 6 },
            new Book { Id = 2, Title = "Book 2 Updated", AuthorId = 1, Price = 11 },
        ]);

        List<Book> list = db.Table<Book>().ToList();

        Assert.Equal(2, list.Count);
        Assert.Equal("Book 1 Updated", list[0].Title);
        Assert.Equal(6, list[0].Price);
        Assert.Equal("Book 2 Updated", list[1].Title);
        Assert.Equal(11, list[1].Price);
    }

    [Fact]
    public void AddOrUpdateRange_NonTransaction()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        db.Table<Book>().AddOrUpdateRange([
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 10 },
        ], runInTransaction: false);

        Assert.Equal(2, db.Table<Book>().Count());
    }

    [Fact]
    public async Task AddOrUpdateAsync_InsertsNewRow()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        await db.Table<Book>().AddOrUpdateAsync(new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 5 });

        List<Book> list = db.Table<Book>().ToList();

        Assert.Single(list);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("Book 1", list[0].Title);
        Assert.Equal(5, list[0].Price);
    }

    [Fact]
    public async Task AddOrUpdateAsync_ReplacesExistingRow()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        await db.Table<Book>().AddOrUpdateAsync(new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 5 });
        await db.Table<Book>().AddOrUpdateAsync(new Book { Id = 1, Title = "Book 1 Updated", AuthorId = 1, Price = 9 });

        List<Book> list = db.Table<Book>().ToList();

        Assert.Single(list);
        Assert.Equal("Book 1 Updated", list[0].Title);
        Assert.Equal(9, list[0].Price);
    }

    [Fact]
    public async Task AddOrUpdateRangeAsync_InsertsNewRows()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        await db.Table<Book>().AddOrUpdateRangeAsync([
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 10 },
        ]);

        List<Book> list = db.Table<Book>().ToList();

        Assert.Equal(2, list.Count);
        Assert.Equal("Book 1", list[0].Title);
        Assert.Equal("Book 2", list[1].Title);
    }

    [Fact]
    public async Task AddOrUpdateRangeAsync_ReplacesExistingRows()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        await db.Table<Book>().AddRangeAsync([
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 10 },
        ]);

        await db.Table<Book>().AddOrUpdateRangeAsync([
            new Book { Id = 1, Title = "Book 1 Updated", AuthorId = 1, Price = 6 },
            new Book { Id = 2, Title = "Book 2 Updated", AuthorId = 1, Price = 11 },
        ]);

        List<Book> list = db.Table<Book>().ToList();

        Assert.Equal(2, list.Count);
        Assert.Equal("Book 1 Updated", list[0].Title);
        Assert.Equal("Book 2 Updated", list[1].Title);
    }
}