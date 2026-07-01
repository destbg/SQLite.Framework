# Transactions

Transactions let you group multiple operations so they either all succeed or all get rolled back. Under the hood, SQLite.Framework uses SQLite savepoints, which means transactions can be nested.

## Basic Usage

Call `BeginTransaction()` and then either `Commit()` or `Rollback()`.

```csharp
await using SQLiteTransaction transaction = await db.BeginTransactionAsync();

try
{
    await db.Table<Author>().AddAsync(new Author { Name = "Robert Martin" });
    await db.Table<Book>().AddAsync(new Book { Title = "Clean Code", AuthorId = 1, Price = 29.99m });

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Auto Rollback on Dispose

If you do not call `Commit()`, the transaction rolls back automatically when the `using` block exits. This means you only need to call `Rollback()` explicitly when you want to roll back early and keep running.

```csharp
async Task AddValues()
{
    await using SQLiteTransaction transaction = await db.BeginTransactionAsync();

    await db.Table<Author>().AddAsync(new Author { Name = "Robert Martin" });
    await db.Table<Book>().AddAsync(new Book { Title = "Clean Code", AuthorId = 1, Price = 29.99m });

    await transaction.CommitAsync(); // Without this line, both inserts are rolled back
}
```

## Early Rollback

Call `Rollback()` whenever you want to undo changes before the `using` block ends:

```csharp
await using SQLiteTransaction transaction = await db.BeginTransactionAsync();

var author = new Author { Name = "Unknown" };
await db.Table<Author>().AddAsync(author);

if (string.IsNullOrEmpty(author.Name))
{
    await transaction.RollbackAsync();
    return;
}

await db.Table<Book>().AddAsync(new Book { Title = "Unnamed", AuthorId = author.Id, Price = 0 });
await transaction.CommitAsync();
```

## Reads Inside a Transaction

Queries (`ToList`, `First`, `Count` and so on) do not acquire the write lock. This means a read can run at any time, even while a transaction is open on another thread. SQLite handles this safely through its own internal locking and, when WAL mode is enabled, through snapshot isolation.

A common pattern where this matters is a background sync that holds a transaction while the UI thread still needs to read:

```csharp
// Background thread
Task syncTask = Task.Run(async () =>
{
    await using SQLiteTransaction transaction = await db.BeginTransactionAsync();
    await db.Table<Book>().AddRangeAsync(newBooks, runInTransaction: false);
    await transaction.CommitAsync();
});

// UI thread - does not wait for syncTask; returns immediately
List<Book> books = await db.Table<Book>().ToListAsync();
```

## Block Reads During a Transaction

By default, a read on another thread runs right away even when a transaction is open. With a separate-connection transaction this means the read sees the data as it was before the transaction started. That snapshot may not match what the transaction will commit, so the read can return a value that is about to change.

If you want reads on other threads to wait until the transaction is done, set `BlockReadsDuringTransaction` on the options. While a transaction is open, any read started from a different async context waits for that transaction to commit or roll back, then runs.

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .UseBlockReadsDuringTransaction()
    .Build();

using var db = new SQLiteDatabase(options);
```

After this is set, the timeline looks like this:

```
// Task A opens a separate-connection transaction
1. BEGIN
2. INSERT INTO Books ...

// Task B starts a read
3. SELECT ... -- waits because Task A's transaction is still open

// Task A commits
4. COMMIT

// Task B's read now runs and sees the inserted row
5. SELECT ... -- returns the new state
```

Things to keep in mind:

- The wait only affects reads from a different async context. Reads from the transaction's own context or from a context that holds the connection lock, do not wait.
- Every read honors the wait.
- The wait is non-blocking, so a thread is not held while the read waits.
- `BlockReadsDuringTransaction` defaults to `false`.

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

## AddRange and UpdateRange

`AddRangeAsync`, `UpdateRangeAsync` and `RemoveRangeAsync` already wrap their operations in a transaction internally. If you are calling them as part of a larger transaction, pass `runInTransaction: false` to avoid nesting unnecessarily.

```csharp
await using SQLiteTransaction transaction = await db.BeginTransactionAsync();

await db.Table<Author>().AddRangeAsync(authors, runInTransaction: false);
await db.Table<Book>().AddRangeAsync(books, runInTransaction: false);

await transaction.CommitAsync();
```
