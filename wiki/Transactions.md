# Transactions

Transactions let you group multiple operations so they either all succeed or all get rolled back. Under the hood, SQLite.Framework uses SQLite savepoints, which means transactions can be nested.

## Basic Usage

Call `BeginTransaction()` and then either `Commit()` or `Rollback()`.

```csharp
await using SQLiteTransaction tx = await db.BeginTransactionAsync();

try
{
    await db.Table<Author>().AddAsync(new Author { Name = "Robert Martin" });
    await db.Table<Book>().AddAsync(new Book { Title = "Clean Code", AuthorId = 1, Price = 29.99m });

    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

## Auto Rollback on Dispose

If you do not call `Commit()`, the transaction rolls back automatically when the `using` block exits. This means you only need to call `Rollback()` explicitly when you want to roll back early and keep running.

```csharp
async Task AddValues()
{
    await using SQLiteTransaction tx = await db.BeginTransactionAsync();

    await db.Table<Author>().AddAsync(new Author { Name = "Robert Martin" });
    await db.Table<Book>().AddAsync(new Book { Title = "Clean Code", AuthorId = 1, Price = 29.99m });

    transaction.Commit(); // Without this line, both inserts are rolled back
}
```

## Early Rollback

Call `Rollback()` whenever you want to undo changes before the `using` block ends:

```csharp
await using SQLiteTransaction tx = await db.BeginTransactionAsync();

var author = new Author { Name = "Unknown" };
await db.Table<Author>().AddAsync(author);

if (string.IsNullOrEmpty(author.Name))
{
    transaction.Rollback();
    return;
}

await db.Table<Book>().AddAsync(new Book { Title = "Unnamed", AuthorId = author.Id, Price = 0 });
transaction.Commit();
```

## Transactions on a Separate Connection

By default a transaction uses the shared database connection and holds the exclusive write lock until it finishes. Pass `separateConnection: true` to run the transaction on its own dedicated connection instead. When you do this, all operations you make in the same async context are automatically routed to that connection, and the shared connection is left free for other threads to read and write without waiting.

```csharp
await using SQLiteTransaction transaction = await db.BeginTransactionAsync(separateConnection: true);

await db.Table<Author>().AddAsync(new Author { Name = "Robert Martin" });
await db.Table<Book>().AddAsync(new Book { Title = "Clean Code", AuthorId = 1, Price = 29.99m });

await transaction.CommitAsync();
```

The sync overload works the same way:

```csharp
using SQLiteTransaction transaction = db.BeginTransaction(separateConnection: true);

db.Table<Author>().Add(new Author { Name = "Robert Martin" });
db.Table<Book>().Add(new Book { Title = "Clean Code", AuthorId = 1, Price = 29.99m });

transaction.Commit();
```

Things to keep in mind when using separate connections:

- Only file-based databases support this. An in-memory database (`:memory:`) cannot be shared across connections, so `separateConnection: true` will not work correctly with one.
- SQLite still only allows one active writer at a time at the file level, so two separate-connection transactions writing simultaneously will still wait on each other, just coordinated by SQLite rather than the ORM.
- Nested transactions (a second `BeginTransaction` call inside an existing one) always use savepoints on the current connection regardless of the flag.

## Reads Inside a Transaction

Queries (`ToList`, `First`, `Count`, and so on) do not acquire the write lock. This means a read can run at any time, even while a transaction is open on another thread. SQLite handles this safely through its own internal locking and, when WAL mode is enabled, through snapshot isolation.

A common pattern where this matters is a background sync that holds a transaction while the UI thread still needs to read:

```csharp
// Background thread
Task syncTask = Task.Run(async () =>
{
    await using SQLiteTransaction tx = await db.BeginTransactionAsync();
    await db.Table<Book>().AddRangeAsync(newBooks, runInTransaction: false);
    transaction.Commit();
});

// UI thread - does not wait for syncTask; returns immediately
List<Book> books = await db.Table<Book>().ToListAsync();
```

## Writes Block While a Transaction Is Open

Only one writer can hold the write lock at a time. If a transaction is already open, any standalone write from another thread will wait until that transaction finishes.

This is intentional. SQLite.Framework maps every transaction to a SQLite savepoint on the same connection. All statements on a connection share the same transaction state, so a standalone write that runs while a savepoint is open is automatically part of that savepoint. When the original transaction rolls back, the standalone write is rolled back with it, even though the calling code never knew it was inside a transaction.

To make this concrete, consider two tasks running on the same `db` instance:

```
// Task A opens a transaction
1. SAVEPOINT sp0;
2. INSERT INTO Authors ...  -- part of sp0

// Task B tries a standalone write while Task A holds the lock
3. INSERT INTO Books ...    -- would silently run inside sp0

// Task A rolls back
4. ROLLBACK TO SAVEPOINT sp0;
-- The Books insert from step 3 is gone too
```

Because step 3 would execute inside `sp0`, it gets rolled back at step 4 without any error. The write lock prevents this by making Task B wait at step 3 until Task A commits or rolls back.

### Working Around the Wait

If you need writes from multiple threads to overlap, create a separate `SQLiteDatabase` instance for each thread and point them at the same database file. SQLite handles the coordination between connections at the file level.

```csharp
// Each background task creates its own connection
await Task.WhenAll(
    Task.Run(() => { using var db1 = new AppDatabase(); db1.Table<Book>().Add(book1); }),
    Task.Run(() => { using var db2 = new AppDatabase(); db2.Table<Book>().Add(book2); })
);
```

If separate connections are not practical, keep transactions as short as possible so other writers wait for less time.

## AddRange and UpdateRange

`AddRangeAsync`, `UpdateRangeAsync`, and `RemoveRangeAsync` already wrap their operations in a transaction internally. If you are calling them as part of a larger transaction, pass `runInTransaction: false` to avoid nesting unnecessarily.

```csharp
await using SQLiteTransaction tx = await db.BeginTransactionAsync();

await db.Table<Author>().AddRangeAsync(authors, runInTransaction: false);
await db.Table<Book>().AddRangeAsync(books, runInTransaction: false);

transaction.Commit();
```
