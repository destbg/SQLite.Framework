# Multi-threading

SQLite.Framework is safe to use from multiple threads and tasks at the same time. You create one `SQLiteDatabase` instance and share it across the app. All database commands acquire a connection lock automatically, so concurrent callers queue up and take turns rather than racing each other.

You do not need to set anything up for this. It works out of the box.

## WAL mode

Calling `UseWalMode()` on the builder switches the database to WAL (Write-Ahead Logging) journal mode. In this mode writes no longer block each other. Multiple writes from different threads can run at the same time, and reads are never blocked by writers.

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .UseWalMode()
    .Build();

using var db = new SQLiteDatabase(options);
```

The framework issues `PRAGMA journal_mode = WAL` automatically when the connection is first opened. Configure the builder before calling `Build()`, so the resulting options are immutable.

With WAL enabled, eight concurrent writes run in parallel instead of queuing up:

```csharp
Task[] tasks = Enumerable.Range(0, 8).Select(async i =>
{
    await db.Table<Book>().AddAsync(new Book { Id = i + 1, Title = $"Book {i}", Price = i + 1 });
}).ToArray();

await Task.WhenAll(tasks);
```

Transactions still behave correctly in WAL mode. Starting a non-separate-connection transaction waits for any writes currently in progress to finish, then takes exclusive access for its duration. Writes that arrive while a transaction is open wait for it to commit or roll back before they proceed.

## How the lock works

The lock lives inside every command execution. When a query or a write reaches the point of actually talking to SQLite, it acquires the lock, does its work, then releases it. Everything before that point (building the query, compiling the expression) happens outside the lock.

In the default mode every write acquires the lock exclusively, so concurrent writes queue up one at a time. Reads never acquire the lock regardless of mode.

In WAL mode multiple writes share the lock concurrently. A non-separate-connection transaction is the only thing that makes writes queue up.

This means you can freely share one `SQLiteDatabase` across threads:

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

Each operation runs on a thread-pool worker. The worker thread is not blocked while another caller holds the connection lock. Once it acquires the lock, the worker runs the SQL inside it.

Between individual operations the lock is released, so other callers can run. If you need a block of operations to stay together without anything else getting in, use a transaction.

## Transactions hold the lock

When you open a transaction, it holds the connection lock for its entire lifetime. No other write can run until the transaction commits or rolls back.

```csharp
await using SQLiteTransaction tx = await db.BeginTransactionAsync();

await db.Table<Book>().AddAsync(new Book { Title = "A", Price = 1 });

var books = await db.Table<Book>().ToListAsync();

await db.Table<Book>().AddAsync(new Book { Title = "B", Price = 2 });

await tx.CommitAsync();
```

Every read and write inside the transaction sees a consistent view of the data, and nothing from another thread can get in between. Once the transaction starts, the lock is held for the rest of the method, so every later operation goes straight through without queuing.

Reads from other threads still run in parallel. If you want them to wait until the transaction is done, set `BlockReadsDuringTransaction` on the options. See the [Transactions](Transactions#block-reads-during-a-transaction) page for details.

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

**Use WAL mode for write-heavy workloads.** Set `IsWalMode = true` to let concurrent writes run in parallel. This is the biggest single throughput improvement available for apps that do many writes at once.

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
