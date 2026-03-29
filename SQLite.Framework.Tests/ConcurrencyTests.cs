using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

// ReSharper disable AccessToDisposedClosure

namespace SQLite.Framework.Tests;

public class ConcurrencyTests
{
    [Fact]
    public async Task EightSyncTasks_EachInsertReadUpdate_AllSucceed()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            int id = i + 1;
            db.Table<Book>().Add(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });

            Book? book = db.Table<Book>().FirstOrDefault(b => b.Id == id);
            Assert.NotNull(book);
            Assert.Equal($"Book {i}", book.Title);

            book.Title = $"Updated {i}";
            db.Table<Book>().Update(book);

            Book? updated = db.Table<Book>().FirstOrDefault(b => b.Id == id);
            Assert.NotNull(updated);
            Assert.Equal($"Updated {i}", updated.Title);
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(8, db.Table<Book>().Count());
    }

    [Fact]
    public async Task EightSyncTasks_NeverHoldLockSimultaneously()
    {
        using ConcurrencyTrackingDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            int id = i + 1;
            db.Table<Book>().Add(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });
            db.Table<Book>().Update(new Book { Id = id, Title = $"Updated {i}", AuthorId = 1, Price = id });
            _ = db.Table<Book>().FirstOrDefault(b => b.Id == id);
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, db.MaxConcurrentLockHolders);
    }

    [Fact]
    public async Task EightAsyncTasks_EachInsertReadUpdate_AllSucceed()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
        {
            int id = i + 1;
            await db.Table<Book>().AddAsync(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });

            Book? book = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();
            Assert.NotNull(book);
            Assert.Equal($"Book {i}", book.Title);

            book.Title = $"Updated {i}";
            await db.Table<Book>().UpdateAsync(book);

            Book? updated = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();
            Assert.NotNull(updated);
            Assert.Equal($"Updated {i}", updated.Title);
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(8, await db.Table<Book>().CountAsync());
    }

    [Fact]
    public async Task EightAsyncTasks_NeverHoldLockSimultaneously()
    {
        using ConcurrencyTrackingDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
        {
            int id = i + 1;
            await db.Table<Book>().AddAsync(new Book { Id = id, Title = $"Book {i}", AuthorId = 1, Price = id });
            await db.Table<Book>().UpdateAsync(new Book { Id = id, Title = $"Updated {i}", AuthorId = 1, Price = id });
            _ = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, db.MaxConcurrentLockHolders);
    }

    [Fact]
    public async Task EightMixedTasks_EachInsertReadUpdate_AllSucceed()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        List<Task> tasks = [];

        for (int i = 0; i < 8; i++)
        {
            int id = i + 1;
            string title = $"Book {i}";
            string updated = $"Updated {i}";

            if (i % 2 == 0)
            {
                tasks.Add(Task.Run(() =>
                {
                    db.Table<Book>().Add(new Book { Id = id, Title = title, AuthorId = 1, Price = id });

                    Book? book = db.Table<Book>().FirstOrDefault(b => b.Id == id);
                    Assert.NotNull(book);
                    Assert.Equal(title, book.Title);

                    book.Title = updated;
                    db.Table<Book>().Update(book);

                    Book? result = db.Table<Book>().FirstOrDefault(b => b.Id == id);
                    Assert.NotNull(result);
                    Assert.Equal(updated, result.Title);
                }));
            }
            else
            {
                tasks.Add(RunAsync(id, title, updated));
            }
        }

        await Task.WhenAll(tasks);

        Assert.Equal(8, db.Table<Book>().Count());

        async Task RunAsync(int id, string title, string updated)
        {
            await db.Table<Book>().AddAsync(new Book { Id = id, Title = title, AuthorId = 1, Price = id });

            Book? book = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();
            Assert.NotNull(book);
            Assert.Equal(title, book.Title);

            book.Title = updated;
            await db.Table<Book>().UpdateAsync(book);

            Book? result = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();
            Assert.NotNull(result);
            Assert.Equal(updated, result.Title);
        }
    }

    [Fact]
    public async Task EightSyncTasks_AddRange_AllSucceed()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            int baseId = i * 3;
            List<Book> books =
            [
                new() { Id = baseId + 1, Title = $"Book {baseId + 1}", AuthorId = 1, Price = baseId + 1 },
                new() { Id = baseId + 2, Title = $"Book {baseId + 2}", AuthorId = 1, Price = baseId + 2 },
                new() { Id = baseId + 3, Title = $"Book {baseId + 3}", AuthorId = 1, Price = baseId + 3 },
            ];
            db.Table<Book>().AddRange(books);
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(24, db.Table<Book>().Count());
    }

    [Fact]
    public async Task EightSyncTasks_AddRange_NeverHoldLockSimultaneously()
    {
        using ConcurrencyTrackingDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            int baseId = i * 3;
            List<Book> books =
            [
                new() { Id = baseId + 1, Title = $"Book {baseId + 1}", AuthorId = 1, Price = baseId + 1 },
                new() { Id = baseId + 2, Title = $"Book {baseId + 2}", AuthorId = 1, Price = baseId + 2 },
                new() { Id = baseId + 3, Title = $"Book {baseId + 3}", AuthorId = 1, Price = baseId + 3 },
            ];
            db.Table<Book>().AddRange(books);
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, db.MaxConcurrentLockHolders);
    }

    [Fact]
    public async Task EightAsyncTasks_AddRangeAsync_AllSucceed()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
        {
            int baseId = i * 3;
            List<Book> books =
            [
                new() { Id = baseId + 1, Title = $"Book {baseId + 1}", AuthorId = 1, Price = baseId + 1 },
                new() { Id = baseId + 2, Title = $"Book {baseId + 2}", AuthorId = 1, Price = baseId + 2 },
                new() { Id = baseId + 3, Title = $"Book {baseId + 3}", AuthorId = 1, Price = baseId + 3 },
            ];
            await db.Table<Book>().AddRangeAsync(books);
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(24, await db.Table<Book>().CountAsync());
    }

    [Fact]
    public async Task EightAsyncTasks_AddRangeAsync_NeverHoldLockSimultaneously()
    {
        using ConcurrencyTrackingDatabase db = new();
        db.Table<Book>().CreateTable();

        Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
        {
            int baseId = i * 3;
            List<Book> books =
            [
                new() { Id = baseId + 1, Title = $"Book {baseId + 1}", AuthorId = 1, Price = baseId + 1 },
                new() { Id = baseId + 2, Title = $"Book {baseId + 2}", AuthorId = 1, Price = baseId + 2 },
                new() { Id = baseId + 3, Title = $"Book {baseId + 3}", AuthorId = 1, Price = baseId + 3 },
            ];
            await db.Table<Book>().AddRangeAsync(books);
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, db.MaxConcurrentLockHolders);
    }

    [Fact]
    public async Task EightMixedTasks_NeverHoldLockSimultaneously()
    {
        using ConcurrencyTrackingDatabase db = new();
        db.Table<Book>().CreateTable();

        List<Task> tasks = [];

        for (int i = 0; i < 8; i++)
        {
            int id = i + 1;
            string title = $"Book {i}";
            string updated = $"Updated {i}";

            if (i % 2 == 0)
            {
                tasks.Add(Task.Run(() =>
                {
                    db.Table<Book>().Add(new Book { Id = id, Title = title, AuthorId = 1, Price = id });
                    db.Table<Book>().Update(new Book { Id = id, Title = updated, AuthorId = 1, Price = id });
                    _ = db.Table<Book>().FirstOrDefault(b => b.Id == id);
                }));
            }
            else
            {
                tasks.Add(RunAsync(id, title, updated));
            }
        }

        await Task.WhenAll(tasks);

        Assert.Equal(1, db.MaxConcurrentLockHolders);

        async Task RunAsync(int id, string title, string updated)
        {
            await db.Table<Book>().AddAsync(new Book { Id = id, Title = title, AuthorId = 1, Price = id });
            await db.Table<Book>().UpdateAsync(new Book { Id = id, Title = updated, AuthorId = 1, Price = id });
            _ = await db.Table<Book>().Where(b => b.Id == id).FirstOrDefaultAsync();
        }
    }
}
