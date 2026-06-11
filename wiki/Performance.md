# Performance

## Head-to-head benchmarks

Against EF Core 10 and sqlite-net-pcl 1.9 on the same in-process SQLite file. 100 rows per operation, .NET 10, BenchmarkDotNet. Lower is better.

**Read 100 rows into a `List<Book>`:**

| ORM | Mean | Allocated |
|---|---:|---:|
| **SQLite.Framework + SourceGenerator** | **34.1 μs** | **22.9 KB** |
| SQLite.Framework | 35.8 μs | 23.0 KB |
| sqlite-net-pcl | 43.3 μs | 15.4 KB |
| EF Core 10 (`AsNoTracking`) | 71.9 μs | 47.6 KB |

**Bulk insert 100 rows (single transaction):**

| ORM | Mean | Allocated |
|---|---:|---:|
| **SQLite.Framework + SourceGenerator** | **130.8 μs** | **13.2 KB** |
| SQLite.Framework | 142.4 μs | 22.5 KB |
| sqlite-net-pcl (`InsertAll`) | 142.4 μs | 20.6 KB |
| EF Core 10 (`AddRange` + `SaveChanges`) | 2,070 μs | 915.9 KB |

**Bulk update 100 rows by predicate:**

| ORM | Mean | Allocated |
|---|---:|---:|
| **SQLite.Framework (`ExecuteUpdate`)** | **143.5 μs** | **16.2 KB** |
| EF Core 10 (`ExecuteUpdate`) | 169.3 μs | 17.0 KB |
| sqlite-net-pcl (`UpdateAll`) | 436.1 μs | 198.3 KB |

**Join + project (1000 Books and 50 Authors, filter `Price > 50`, sort, project to a DTO with a sub-query in the projection):**

| ORM | Mean | Allocated |
|---|---:|---:|
| **SQLite.Framework** | **74.0 μs** | **39.6 KB** |
| SQLite.Framework + SourceGenerator | 78.5 μs | 45.1 KB |
| EF Core 10 | 126.3 μs | 76.3 KB |
| sqlite-net-pcl | 687.2 μs | 203.9 KB |

sqlite-net-pcl's `TableQuery<T>` is `IEnumerable<T>`, not `IQueryable<T>`, so the LINQ join binds to `Enumerable.Join`. The whole `Books` and `Authors` tables load into memory before the filter and join run client-side.

The benchmark project lives at [`Sample/SQLite.Framework.Benchmarks`](https://github.com/destbg/SQLite.Framework/tree/main/Sample/SQLite.Framework.Benchmarks) and can be reproduced with `dotnet run --project Sample/SQLite.Framework.Benchmarks -c Release`.

## On-device benchmarks (Android)

The same four operations measured on a real Android device. BenchmarkDotNet does not run on Android, so this uses a simple `Stopwatch`. The numbers are rough and shown only as an example. Lower is better.

| ORM | Read 100 | Insert 100 | Update 100 | Join + sub-query |
|---|---:|---:|---:|---:|
| **SQLite.Framework + SourceGenerator** | **893** | 4,882 | 5,533 | **1,900** |
| SQLite.Framework | 1,050 | **4,851** | **4,968** | 1,902 |
| sqlite-net-pcl | 999 | 5,566 | 8,861 | 13,169 |
| EF Core 10 | 3,281 | 28,882 | 6,296 | 6,971 |

The harness lives at [`Sample/SQLite.Framework.AndroidBench`](https://github.com/destbg/SQLite.Framework/tree/main/Sample/SQLite.Framework.AndroidBench). Launching it runs all four. To run one ORM use `adb shell am start -n com.sqliteframework.androidbench/.MainActivity --es orm ef` (values `framework`, `frameworkgen`, `ef`, `sqlitenet`, or `all`).

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

await tx.CommitAsync();
```

**Reuse the SQLiteDatabase instance**

The connection is lazy and opens once. Creating a new `SQLiteDatabase` for every operation is wasteful. Create it once and keep it alive for the lifetime of your app or service.

**Use WITHOUT ROWID for lookup tables**

If a table is mostly looked up by primary key and rarely scanned in order, `[WithoutRowId]` can reduce the number of B-tree lookups. See [Defining Models](Defining%20Models) for usage.

## Inspecting the query plan

`IQueryable<T>.ExplainQueryPlan()` runs `EXPLAIN QUERY PLAN` on the query and returns the result as a tree of `SQLiteQueryPlanNode`.

```csharp
SQLiteQueryPlan plan = await db.Table<Book>()
    .Where(b => b.AuthorId == 1)
    .ExplainQueryPlanAsync();

Console.WriteLine(plan);
```

prints

```
QUERY PLAN
> SEARCH b0 USING INDEX IX_Book_AuthorId (BookAuthorId=?)
```

`SQLiteQueryPlan.ToString()` renders the tree as ASCII text. To inspect the tree directly, walk `plan.Roots` and `node.Children`.

A non-indexed predicate gives a `SCAN` instead of a `SEARCH`.
