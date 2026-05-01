# Migrating from sqlite-net-pcl

This page covers some of the key differences between `sqlite-net-pcl` and `SQLite.Framework` to help you move an existing project over.

## LINQ Select Is Fully Supported

`sqlite-net-pcl` does not translate `Select` projections to SQL. It supports `Where`, `OrderBy`, `Take`, `Skip`, and `Count` in SQL, but it always fetches every column from the database. Any `Select` projection is applied in memory on the client after the full rows are read. This means that even if you only need two columns from a twenty-column table, you pay the cost of reading all twenty.

`SQLite.Framework` translates `Select` to SQL, so only the columns you ask for are sent over the wire:

```csharp
// sqlite-net-pcl reads all columns, then picks Title and Price in memory
var summaries = db.Table<Book>().Select(b => new { b.Title, b.Price }).ToList();

// SQLite.Framework generates SELECT BookTitle, BookPrice FROM Books
var summaries = await db.Table<Book>()
    .Select(b => new { b.Title, b.Price })
    .ToListAsync();
```

## Migrating Existing Raw SQL Queries

If you have raw SQL queries from `sqlite-net-pcl` that return fewer columns than the full model, `FromSql` will throw by default because it generates an outer `SELECT` that references every mapped column. You have two options:

**Option 1** -> project into a smaller type that only declares the columns you select:

```csharp
public class BookSummary
{
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
}

var result = await db.FromSql<BookSummary>(
    "SELECT BookTitle AS Title, BookPrice AS Price FROM Books"
).ToListAsync();
```

**Option 2** -> use `Query<T>` instead of `FromSql<T>`. It does not wrap your SQL in a subquery, so it has no column list to check against. Columns not returned by the query are left at their default values:

```csharp
// Missing columns on the model are null/default instead of throwing
List<Book> books = db.Query<Book>(
    "SELECT BookId AS Id, BookTitle AS Title FROM Books"
);
```

## Data Types

Most types are stored the same way by both libraries and need no attention. A few require care.

If your project used non-default sqlite-net-pcl storage settings, you can configure `SQLite.Framework` to write data in the same format. This means new rows will be consistent with existing ones and you do not need to migrate your database at all:

```csharp
// Match sqlite-net-pcl settings where StoreDateTimeAsTicks = false and StoreTimeSpanAsTicks = false
SQLiteOptions options = new SQLiteOptionsBuilder("myapp.db3")
    .UseDateTimeStorage(DateTimeStorageMode.TextFormatted)
    .UseTimeSpanStorage(TimeSpanStorageMode.Text)
    .Build();

using SQLiteDatabase db = new(options);
```

If some of your tables use one format and others use a different format, you can still read all of them. `SQLite.Framework` detects the stored format when reading and handles both. The options only control what format is used when writing.

### DateTime

`sqlite-net-pcl` has a `StoreDateTimeAsTicks` option:

- `true` (the default) stores as an INTEGER tick count. This matches `SQLite.Framework`'s default, so no changes are needed.
- `false` stores as a formatted date string like `2023-06-15 12:00:00`. Set `DateTimeStorage = DateTimeStorageMode.TextFormatted` to write in the same format.

There is also an older sqlite-net-pcl behavior where the tick count was stored as a TEXT string instead of an INTEGER. Use `DateTimeStorage = DateTimeStorageMode.TextTicks` to write in that format.

When `DateTimeStorage` is set to `TextFormatted`, LINQ property access like `.Year` and `.Month` is not supported in `Where` and `OrderBy`. It does work in `Select` because the value is fetched and the property is computed in C# after the query runs.

### TimeSpan

`sqlite-net-pcl` has a `StoreTimeSpanAsTicks` option:

- `true` (the default) stores as an INTEGER tick count. This matches `SQLite.Framework`'s default, so no changes are needed.
- `false` stores as a string in the standard format `1.02:03:04.5678900`. Set `TimeSpanStorage = TimeSpanStorageMode.Text` to write in the same format.

When `TimeSpanStorage` is set to `Text`, LINQ property access like `.Days` and `.TotalHours` is not supported in `Where` and `OrderBy`. It does work in `Select` because the value is fetched and the property is computed in C# after the query runs.

### Enum

`sqlite-net-pcl` supports a `[StoreAsText]` attribute that stores enum values as their name instead of their number. Set `EnumStorage = EnumStorageMode.Text` to write enums as text names.

Note that `sqlite-net-pcl`'s `[StoreAsText]` is applied per enum type, while `SQLite.Framework`'s `EnumStorage` is a global setting that applies to all enums. If your existing database has a mix of text and integer enums, reading still works correctly because `SQLite.Framework` detects the format automatically. Writing consistently will require that all enums use the same format.

### DateTimeOffset

Both libraries store `DateTimeOffset` as a tick count and always read it back with a zero offset. If your existing data only contains UTC values, everything works as expected. If you stored non-UTC offsets, the offset will be zero after reading in both libraries.

`SQLite.Framework` also supports `DateTimeOffsetStorage = DateTimeOffsetStorageMode.UtcTicks`, which stores the UTC tick count instead of the local tick count. This is the safer option for new databases where you want consistent UTC behavior.

### Types not in sqlite-net-pcl

`SQLite.Framework` supports `DateOnly`, `TimeOnly`, and `char`. These do not exist in sqlite-net-pcl, so there is nothing to migrate.

## Other Differences

| Feature | sqlite-net-pcl | SQLite.Framework |
|---|---|---|
| LINQ `Select` projection | In memory after full row fetch | Translated to SQL |
| LINQ `Where`, `OrderBy`, `Take`, `Skip` | Translated to SQL | Translated to SQL |
| Joins | Pulls both tables in memory | Translated to SQL |
| Group by / aggregates | Pulls table in memory | Translated to SQL |
| Subqueries | Not supported | Translated to SQL |
| Async support | Separate table | Extension method |
