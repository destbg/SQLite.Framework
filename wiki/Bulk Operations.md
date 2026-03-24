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

## Sync Versions

All of the above have synchronous versions:

```csharp
db.Table<Book>().ExecuteDelete();
db.Table<Book>().ExecuteDelete(b => b.InStock == false);

db.Table<Book>()
    .Where(b => b.Price < 5)
    .ExecuteUpdate(s => s.Set(b => b.InStock, false));
```
