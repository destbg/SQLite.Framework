using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class InsertFromQueryTests
{
    [Fact]
    public void InsertFromQuery_SameShape_CopiesRowsAndReturnsCount()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<BookArchive>();

        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "c", AuthorId = 2, Price = 3 },
        });

        int inserted = db.Table<BookArchive>().InsertFromQuery(
            db.Table<Book>().Select(b => new BookArchive
            {
                Id = b.Id,
                Title = b.Title,
                AuthorId = b.AuthorId,
                Price = b.Price,
            }));

        Assert.Equal(3, inserted);

        List<BookArchive> rows = db.Table<BookArchive>().OrderBy(b => b.Id).ToList();
        Assert.Equal([1, 2, 3], rows.Select(b => b.Id));
        Assert.Equal(["a", "b", "c"], rows.Select(b => b.Title));
    }

    [Fact]
    public void InsertFromQuery_WithWhere_FiltersBeforeInsert()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<BookArchive>();

        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "c", AuthorId = 2, Price = 3 },
        });

        int inserted = db.Table<BookArchive>().InsertFromQuery(
            db.Table<Book>()
                .Where(b => b.Price >= 2)
                .Select(b => new BookArchive
                {
                    Id = b.Id,
                    Title = b.Title,
                    AuthorId = b.AuthorId,
                    Price = b.Price,
                }));

        Assert.Equal(2, inserted);

        List<BookArchive> rows = db.Table<BookArchive>().OrderBy(b => b.Id).ToList();
        Assert.Equal([2, 3], rows.Select(b => b.Id));
    }

    [Fact]
    public void InsertFromQuery_NullSource_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<BookArchive>();

        Assert.Throws<ArgumentNullException>(() => db.Table<BookArchive>().InsertFromQuery(null!));
    }

    [Fact]
    public void InsertFromQuery_DoesNotRunHooks()
    {
        int hookCalls = 0;

        using TestDatabase db = new(builder => builder.OnAdd<BookArchive>(_ => { hookCalls++; }));
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<BookArchive>();

        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 },
        });

        db.Table<BookArchive>().InsertFromQuery(
            db.Table<Book>().Select(b => new BookArchive
            {
                Id = b.Id,
                Title = b.Title,
                AuthorId = b.AuthorId,
                Price = b.Price,
            }));

        Assert.Equal(0, hookCalls);
        Assert.Equal(2, db.Table<BookArchive>().Count());
    }

    [Fact]
    public async Task InsertFromQueryAsync_CopiesRowsAndReturnsCount()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<BookArchive>();

        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 },
        });

        int inserted = await db.Table<BookArchive>().InsertFromQueryAsync(
            db.Table<Book>().Select(b => new BookArchive
            {
                Id = b.Id,
                Title = b.Title,
                AuthorId = b.AuthorId,
                Price = b.Price,
            }), TestContext.Current.CancellationToken);

        Assert.Equal(2, inserted);
        Assert.Equal(2, db.Table<BookArchive>().Count());
    }
}
