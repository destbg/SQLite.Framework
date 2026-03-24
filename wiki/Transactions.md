# Transactions

Transactions let you group multiple operations so they either all succeed or all get rolled back. Under the hood, SQLite.Framework uses SQLite savepoints, which means transactions can be nested.

## Basic Usage

Call `BeginTransaction()` and then either `Commit()` or `Rollback()`.

```csharp
using SQLiteTransaction transaction = db.BeginTransaction();

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
    using SQLiteTransaction transaction = db.BeginTransaction();

    await db.Table<Author>().AddAsync(new Author { Name = "Robert Martin" });
    await db.Table<Book>().AddAsync(new Book { Title = "Clean Code", AuthorId = 1, Price = 29.99m });

    transaction.Commit(); // Without this line, both inserts are rolled back
}
```

## Early Rollback

Call `Rollback()` whenever you want to undo changes before the `using` block ends:

```csharp
using SQLiteTransaction transaction = db.BeginTransaction();

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

## AddRange and UpdateRange

`AddRangeAsync`, `UpdateRangeAsync`, and `RemoveRangeAsync` already wrap their operations in a transaction internally. If you are calling them as part of a larger transaction, pass `runInTransaction: false` to avoid nesting unnecessarily.

```csharp
using SQLiteTransaction transaction = db.BeginTransaction();

await db.Table<Author>().AddRangeAsync(authors, runInTransaction: false);
await db.Table<Book>().AddRangeAsync(books, runInTransaction: false);

transaction.Commit();
```
