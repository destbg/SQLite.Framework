# Multi-threading

SQLite.Framework is safe to use from multiple threads and tasks at the same time. You create one `SQLiteDatabase` instance and share it across the app. All database commands acquire a connection lock automatically, so concurrent callers queue up and take turns rather than racing each other.

You do not need to set anything up for this. It works out of the box.

## How the lock works

The lock lives inside every command execution. When a query or a write reaches the point of actually talking to SQLite, it acquires the lock, does its work, then releases it. Everything before that point (building the query, compiling the expression) happens outside the lock.

This means you can freely share one `SQLiteDatabase` across threads:

```csharp
Task[] tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
{
    db.Table<Book>().Add(new Book { Id = i + 1, Title = $"Book {i}", Price = i + 1 });

    var book = db.Table<Book>().First(b => b.Id == i + 1);
    book.Title = $"Updated {i}";
    db.Table<Book>().Update(book);
})).ToArray();

await Task.WhenAll(tasks);
```

Async operations work the same way. They run the sync operation on a background thread, and the lock is acquired inside the command just like in the sync case:

```csharp
Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
{
    await db.Table<Book>().AddAsync(new Book { Id = i + 1, Title = $"Book {i}", Price = i + 1 });

    var book = await db.Table<Book>().Where(b => b.Id == i + 1).FirstAsync();
    book.Title = $"Updated {i}";
    await db.Table<Book>().UpdateAsync(book);
}).ToArray();

await Task.WhenAll(tasks);
```

Between individual operations the lock is released, so other callers can run. If you need a block of operations to stay together without anything else getting in, use a transaction.

## Transactions hold the lock

When you open a transaction, it holds the connection lock for its entire lifetime. No other operation can run until the transaction commits or rolls back.

```csharp
using SQLiteTransaction tx = db.BeginTransaction();

db.Table<Book>().Add(new Book { Title = "A", Price = 1 });
db.Table<Book>().Add(new Book { Title = "B", Price = 2 });

tx.Commit();
```

Every read and write inside the transaction sees a consistent view of the data, and nothing from another thread can get in between.

## Async transactions

Use `BeginTransactionAsync` when you are in an async method. It waits for the lock without blocking a thread.

```csharp
await using SQLiteTransaction tx = await db.BeginTransactionAsync();

await db.Table<Book>().AddAsync(new Book { Title = "A", Price = 1 });

var books = await db.Table<Book>().ToListAsync();

await db.Table<Book>().AddAsync(new Book { Title = "B", Price = 2 });

await tx.CommitAsync();
```

Awaited queries inside an async transaction work correctly. Once `BeginTransactionAsync` completes, the lock is held for the rest of the async method, so every subsequent operation like `ToListAsync`, `AddAsync`, and `UpdateAsync` goes straight through without queuing.

## Running transactions in parallel

You can start many transactions at the same time. Each one waits for the previous to finish, which is exactly what you want from a single SQLite connection.

```csharp
Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
{
    await using SQLiteTransaction tx = await db.BeginTransactionAsync();

    await db.Table<Book>().AddAsync(new Book { Id = i + 1, Title = $"Book {i}", Price = i + 1 });

    var book = await db.Table<Book>().Where(b => b.Id == i + 1).FirstAsync();
    book.Title = $"Updated {i}";
    await db.Table<Book>().UpdateAsync(book);

    await tx.CommitAsync();
}).ToArray();

await Task.WhenAll(tasks);
```

## Tips

**Keep transactions short.** While a transaction holds the lock, everything else waits. Do not do network calls, file I/O, or other slow work between `BeginTransaction` and `Commit`.

**Use `BeginTransactionAsync` in async code.** The sync `BeginTransaction` blocks the thread while waiting for the lock. `BeginTransactionAsync` yields instead.

**Use `runInTransaction: false` inside a transaction.** `AddRangeAsync`, `UpdateRangeAsync`, and `RemoveRangeAsync` open their own internal transaction by default. If you are already inside one, pass `runInTransaction: false`.

```csharp
await using SQLiteTransaction tx = await db.BeginTransactionAsync();

await db.Table<Author>().AddRangeAsync(authors, runInTransaction: false);
await db.Table<Book>().AddRangeAsync(books, runInTransaction: false);

await tx.CommitAsync();
```

**Do not share a transaction across threads.** A transaction belongs to the async flow that opened it. Reading and writing through the same database instance from other threads is fine, but passing a `SQLiteTransaction` object to another thread is not supported.
