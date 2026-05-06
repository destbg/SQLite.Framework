using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AsyncTableMutationTests
{
    [Fact]
    public async Task RemoveAsync_DeletesRow()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        Book b = new() { Id = 1, Title = "A", AuthorId = 1, Price = 1 };
        db.Table<Book>().Add(b);

        int affected = await db.Table<Book>().RemoveAsync(b);

        Assert.Equal(1, affected);
        Assert.Empty(db.Table<Book>().ToList());
    }

    [Fact]
    public async Task UpsertAsync_InsertsRow()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        Book b = new() { Id = 1, Title = "A", AuthorId = 1, Price = 1 };

        int affected = await db.Table<Book>().UpsertAsync(b, c => c.OnConflict(x => x.Id).DoNothing());

        Assert.Equal(1, affected);
        Assert.Single(db.Table<Book>().ToList());
    }

    [Fact]
    public async Task UpsertAsync_OnConflict_DoesNothing()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Original", AuthorId = 1, Price = 1 });

        await db.Table<Book>().UpsertAsync(
            new Book { Id = 1, Title = "Updated", AuthorId = 1, Price = 1 },
            c => c.OnConflict(x => x.Id).DoNothing());

        Assert.Equal("Original", db.Table<Book>().Single().Title);
    }

    [Fact]
    public async Task ClearAsync_DeletesAll()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        });

        int affected = await db.Table<Book>().ClearAsync();

        Assert.Equal(2, affected);
        Assert.Empty(db.Table<Book>().ToList());
    }

    [Fact]
    public async Task UpsertRangeAsync_InsertsRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        int affected = await db.Table<Book>().UpsertRangeAsync(
            new[]
            {
                new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
                new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            },
            c => c.OnConflict(x => x.Id).DoNothing());

        Assert.Equal(2, affected);
        Assert.Equal(2, db.Table<Book>().Count());
    }

    [Fact]
    public async Task UpsertRangeAsync_NoTransaction_InsertsRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        int affected = await db.Table<Book>().UpsertRangeAsync(
            new[]
            {
                new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
                new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            },
            c => c.OnConflict(x => x.Id).DoNothing(),
            runInTransaction: false);

        Assert.Equal(2, affected);
    }

    [Fact]
    public async Task AddRangeAsync_DuplicateKey_RollsBackTransaction()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Existing", AuthorId = 1, Price = 1 });

        Book[] batch =
        [
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 1, Title = "Conflict", AuthorId = 1, Price = 3 },
        ];

        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await db.Table<Book>().AddRangeAsync(batch, ct: TestContext.Current.CancellationToken));

        Assert.Equal(1, db.Table<Book>().Count());
        Assert.Equal("Existing", db.Table<Book>().Single().Title);
    }

    [Fact]
    public async Task AddRangeAsync_NoTransaction_InsertsRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        int affected = await db.Table<Book>().AddRangeAsync(
            new[]
            {
                new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
                new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            },
            runInTransaction: false);

        Assert.Equal(2, affected);
    }

#pragma warning disable CS0618 // Obsolete shim
    [Fact]
    public async Task CreateTableAsync_ObsoleteShim_CreatesTable()
    {
        using TestDatabase db = new();
        await db.Table<Book>().CreateTableAsync();
        Assert.True(db.Schema.TableExists<Book>());
    }

    [Fact]
    public async Task DropTableAsync_ObsoleteShim_DropsTable()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        await db.Table<Book>().DropTableAsync();
        Assert.False(db.Schema.TableExists<Book>());
    }
#pragma warning restore CS0618
}
