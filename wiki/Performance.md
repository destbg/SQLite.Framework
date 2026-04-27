# Performance

## Benchmark: Bulk Insert

Inserting 1000 rows with a transaction takes a fraction of the time compared to inserting them one by one without one. SQLite commits each write to disk by default, so individual inserts are slow.

```csharp
var entities = Enumerable.Range(0, 1000)
    .Select(i => new Book { Title = $"Book {i}", AuthorId = 1, Price = 9.99m })
    .ToList();

// Fast: all inserts in a single transaction (default behavior)
await db.Table<Book>().AddRangeAsync(entities);

// Slow: one transaction per insert
await db.Table<Book>().AddRangeAsync(entities, runInTransaction: false);
```

On a typical device, inserting 1000 rows with a transaction completes in under 0.1 seconds. Without a transaction it can take 5 to 10 seconds or more (depending on the device).

## Tips

**Use AddRange for bulk inserts**

`AddRangeAsync` wraps everything in a single transaction by default. Do not call `AddAsync` in a loop.

```csharp
// Good
await db.Table<Book>().AddRangeAsync(books);

// Slow
foreach (var book in books)
    await db.Table<Book>().AddAsync(book);
```

**Use ExecuteDelete and ExecuteUpdate for bulk changes**

These run a single SQL statement. Loading rows into memory to update or delete them one by one is much slower.

```csharp
// Good: one SQL statement
await db.Table<Book>()
    .Where(b => b.InStock == false)
    .ExecuteDeleteAsync();

// Slow: loads all rows, then deletes each one
var stale = await db.Table<Book>().Where(b => b.InStock == false).ToListAsync();
await db.Table<Book>().RemoveRangeAsync(stale);
```

**Select only the columns you need**

Projecting to a smaller type reduces the data SQLite has to read and the objects .NET has to allocate.

```csharp
// Only fetches Title and Price columns
var summaries = await db.Table<Book>()
    .Select(b => new { b.Title, b.Price })
    .ToListAsync();
```

**Add indexes for columns you filter or sort on**

Without an index, SQLite scans the whole table for every query. Add `[Indexed]` to columns that appear in `Where`, `OrderBy`, or `Join` conditions.

```csharp
public class Book
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [Indexed(Name = "IX_Book_AuthorId")]
    public int AuthorId { get; set; }

    [Indexed(Name = "IX_Book_Price")]
    public decimal Price { get; set; }
}
```

**Wrap related operations in a transaction**

If you are inserting or updating rows across multiple tables as part of one logical operation, put them in a transaction. This is both safer (all or nothing) and faster (one disk commit instead of many).

```csharp
await using SQLiteTransaction tx = await db.BeginTransactionAsync();

await db.Table<Author>().AddRangeAsync(authors, runInTransaction: false);
await db.Table<Book>().AddRangeAsync(books, runInTransaction: false);

transaction.Commit();
```

**Reuse the SQLiteDatabase instance**

The connection is lazy and opens once. Creating a new `SQLiteDatabase` for every operation is wasteful. Create it once and keep it alive for the lifetime of your app or service.

**Use the source generator on hot read paths**

For `Select` projections and entity reads, the source generator emits typed reader code that skips the runtime expression-tree walk and the per-cell boxing the reflection path does. On a small list query (a `Where` plus `ToList` returning ~50 rows) it is up to **24% faster** and uses up to **37% less allocated memory** than the runtime path. See [Source Generator](Source%20Generator) for the full numbers and how to enable it.

**Use WITHOUT ROWID for lookup tables**

If a table is mostly looked up by primary key and rarely scanned in order, `[WithoutRowId]` can reduce the number of B-tree lookups. See [Defining Models](Defining%20Models) for usage.
