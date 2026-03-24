# CRUD Operations

Get a table reference with `db.Table<T>()`. All operations return the number of rows affected.

```csharp
var books = db.Table<Book>();
```

## Create Table

```csharp
await books.CreateTableAsync();
```

Uses `CREATE TABLE IF NOT EXISTS`, so it is safe to call on every startup. If you have `[Indexed]` attributes on your model, the indexes are created at the same time.

## Drop Table

```csharp
await books.DropTableAsync();
```

Uses `DROP TABLE IF EXISTS`.

## Add

```csharp
var book = new Book { Title = "Clean Code", AuthorId = 1, Price = 29.99m };
await books.AddAsync(book);
```

If the primary key has `[AutoIncrement]`, SQLite assigns the value and the property on your object is not updated. To get the new ID back you need a follow-up query.

## Add Many

```csharp
var newBooks = new List<Book>
{
    new() { Title = "Clean Code", AuthorId = 1, Price = 29.99m },
    new() { Title = "The Pragmatic Programmer", AuthorId = 2, Price = 35.00m },
    new() { Title = "Refactoring", AuthorId = 1, Price = 40.00m },
};

await books.AddRangeAsync(newBooks);
```

`AddRangeAsync` wraps all inserts in a transaction by default for better performance. Pass `runInTransaction: false` if you want to add them one by one.

## Update

```csharp
var book = await books.FirstAsync(b => b.Id == 1);
book.Price = 24.99m;

await books.UpdateAsync(book);
```

Update matches the row by primary key. Every other column is updated.

## Update Many

```csharp
var list = await books.Where(b => b.AuthorId == 1).ToListAsync();

foreach (var book in list)
    book.Price *= 0.9m;

await books.UpdateRangeAsync(list);
```

Like `AddRangeAsync`, this runs in a transaction by default.

## Remove

```csharp
var book = await books.FirstAsync(b => b.Id == 1);
await books.RemoveAsync(book);
```

The model must have a `[Key]` property, Remove matches the row by that key. To remove rows in tables without a `[Key]` property, see [Bulk Operations](Bulk%20Operations).

## Remove Many

```csharp
var toDelete = await books.Where(b => b.InStock == false).ToListAsync();
await books.RemoveRangeAsync(toDelete);
```

## Clear All Rows

```csharp
await books.ClearAsync();
```

Deletes every row in the table. The table itself does not get deleted, for that use the `DropTableAsync()` method.

## Sync Versions

Every method above has a synchronous version without the `Async` suffix:

```csharp
books.Add(book);
books.AddRange(newBooks);
books.Update(book);
books.UpdateRange(list);
books.Remove(book);
books.RemoveRange(toDelete);
books.Clear();
books.CreateTable();
books.DropTable();
```
