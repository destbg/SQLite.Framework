# Bulk Operations

`ExecuteDelete` and `ExecuteUpdate` run a single SQL statement that affects many rows at once. This is much faster than loading records into memory and calling `Remove` or `Update` on each one.

## Delete All Rows

```csharp
await db.Table<Book>().ExecuteDeleteAsync();
```

This deletes every row in the table. If you want to delete with a condition, see below.

## Delete with a Condition

```csharp
await db.Table<Book>().ExecuteDeleteAsync(b => b.InStock == false);
```

You can also filter first using `Where` and then call `ExecuteDeleteAsync` without a predicate:

```csharp
await db.Table<Book>()
    .Where(b => b.Price < 5 && b.InStock == false)
    .ExecuteDeleteAsync();
```

Both produce the same SQL.

## Update All Rows

Use `ExecuteUpdateAsync` with a `Set` call to update one or more columns.

```csharp
// Set a fixed value
await db.Table<Book>().ExecuteUpdateAsync(s => s
    .Set(b => b.InStock, true)
);
```

## Update with a Condition

Filter with `Where` before calling `ExecuteUpdateAsync`:

```csharp
await db.Table<Book>()
    .Where(b => b.Genre == "Fiction")
    .ExecuteUpdateAsync(s => s
        .Set(b => b.InStock, false)
    );
```

## Update Using the Existing Value

The second overload of `Set` takes an expression so you can reference the current column value.

```csharp
// Apply a 10% discount to all books under $50
await db.Table<Book>()
    .Where(b => b.Price < 50)
    .ExecuteUpdateAsync(s => s
        .Set(b => b.Price, b => b.Price * 0.9m)
    );
```

## Update Multiple Columns

Chain multiple `Set` calls:

```csharp
await db.Table<Book>()
    .Where(b => b.AuthorId == 3)
    .ExecuteUpdateAsync(s => s
        .Set(b => b.InStock, false)
        .Set(b => b.Price, b => b.Price * 1.1m)
    );
```

## Insert Rows From a Query

`InsertFromQuery` copies rows from one query into a table using a single `INSERT INTO ... SELECT` statement. The data never round-trips through your code, so this is the fastest way to copy or archive rows.

```csharp
// Copy all out of stock books into the archive table
db.Table<BookArchive>().InsertFromQuery(
    db.Table<Book>()
        .Where(b => b.InStock == false)
        .Select(b => new BookArchive
        {
            Id = b.Id,
            Title = b.Title,
            AuthorId = b.AuthorId,
            Price = b.Price,
        }));
```

The source must be a query from the same database. The columns are inserted in the table's column order, so primary keys from the source are preserved. `OnAdd` hooks do not fire for the inserted rows, the same as `ExecuteUpdate` and `ExecuteDelete`.

The async version is `InsertFromQueryAsync`:

```csharp
await db.Table<BookArchive>().InsertFromQueryAsync(
    db.Table<Book>().Where(b => b.InStock == false).Select(b => new BookArchive { ... }));
```

## Sync Versions

All of the above have synchronous versions:

```csharp
db.Table<Book>().ExecuteDelete();
db.Table<Book>().ExecuteDelete(b => b.InStock == false);

db.Table<Book>()
    .Where(b => b.Price < 5)
    .ExecuteUpdate(s => s.Set(b => b.InStock, false));
```
