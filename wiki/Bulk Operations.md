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

## Update from a Joined Table

`ExecuteUpdate` accepts a `Join` chain so you can pull values from another table into the update. SQLite emits this as `UPDATE target SET ... FROM source WHERE ...`. Requires SQLite 3.33 or later.

```csharp
await db.Table<Book>()
    .Join(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => new { b, a })
    .Where(x => x.a.Country == "US")
    .ExecuteUpdateAsync(s => s.Set(x => x.b.Title, x => x.a.Name + " - bestseller"));
```

The `Set` left value must point at the target table (the first `Table<T>` in the chain). The right value can read any column from the joined sources. Chaining more `Join` calls produces a comma-separated `FROM` list.

## Insert Rows From a Query

`InsertFromQuery` copies rows from one query into a table using a single `INSERT INTO ... SELECT` statement. The data never round-trips through your code, so this is the fastest way to copy or archive rows.

```csharp
// Copy all out of stock books into the archive table
await db.Table<BookArchive>().InsertFromQueryAsync(
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

## Returning the Affected Rows

`Returning` wraps a query so the next `ExecuteDelete` or `ExecuteUpdate` emits a SQLite `RETURNING` clause and returns the affected rows. Requires SQLite 3.35 or later.

```csharp
List<Book> deleted = await db.Table<Book>()
    .Where(b => b.InStock == false)
    .Returning()
    .ExecuteDeleteAsync();

List<int> archivedIds = await db.Table<Book>()
    .Where(b => b.Price > 100)
    .Returning(b => b.Id)
    .ExecuteDeleteAsync();

List<decimal> newPrices = await db.Table<Book>()
    .Where(b => b.AuthorId == 3)
    .Returning(b => b.Price)
    .ExecuteUpdateAsync(s => s.Set(b => b.Price, b => b.Price * 1.1m));
```

The `RETURNING` clause can only reference columns from the table being modified. If the projection touches a joined entity, SQLite rejects the statement.
