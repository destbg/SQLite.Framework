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

## Raw SQL as a Projection Workaround

Because `sqlite-net-pcl` cannot project in SQL, a common pattern there is to write a raw SQL query that only selects the columns you need and map the result into a smaller class:

```csharp
// sqlite-net-pcl workaround
var summaries = db.Query<BookSummary>("SELECT BookTitle AS Title, BookPrice AS Price FROM Books");
```

In `SQLite.Framework` you do not need this workaround. Use `Select` directly on the table:

```csharp
public class BookSummary
{
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
}

var summaries = await db.Table<Book>()
    .Select(b => new BookSummary { Title = b.Title, Price = b.Price })
    .ToListAsync();
```

Or use `FromSql` with a narrower type if you still prefer raw SQL:

```csharp
var summaries = await db.FromSql<BookSummary>(
    "SELECT BookTitle AS Title, BookPrice AS Price FROM Books"
).ToListAsync();
```

## Migrating Existing Raw SQL Queries

If you have raw SQL queries from `sqlite-net-pcl` that return fewer columns than the full model, `FromSql` will throw by default because it generates an outer `SELECT` that references every mapped column. You have two options:

**Option 1** -> project into a smaller type that only declares the columns you select (recommended):

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

## Other Differences

| Feature | sqlite-net-pcl | SQLite.Framework |
|---|---|---|
| LINQ `Select` projection | In memory after full row fetch | Translated to SQL |
| LINQ `Where`, `OrderBy`, `Take`, `Skip` | Translated to SQL | Translated to SQL |
| Joins | Pulls both tables in memory | Translated to SQL |
| Group by / aggregates | Pulls table in memory | Translated to SQL |
| Subqueries | Not supported | Translated to SQL |
| Async support | Separate table | Extension method |
