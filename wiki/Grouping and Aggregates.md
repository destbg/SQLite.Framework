# Grouping and Aggregates

## Aggregates Without Grouping

These run directly on a table or a filtered query.

```csharp
int total = await db.Table<Book>().CountAsync();

decimal totalSpend = await db.Table<Book>().SumAsync(b => b.Price);

decimal avg = await db.Table<Book>().AverageAsync(b => b.Price);

decimal cheapest = await db.Table<Book>().MinAsync(b => b.Price);

decimal mostExpensive = await db.Table<Book>().MaxAsync(b => b.Price);
```

Combine with `Where` to aggregate a subset:

```csharp
decimal avgForAuthor = await db.Table<Book>()
    .Where(b => b.AuthorId == 1)
    .AverageAsync(b => b.Price);
```

## Group By

Use query syntax to group records and project an aggregate per group.

```csharp
var countByGenre = await (
    from book in db.Table<Book>()
    group book by book.Genre into g
    select new { Genre = g.Key, Count = g.Count() }
).ToListAsync();
```

```csharp
var totalByAuthor = await (
    from book in db.Table<Book>()
    group book by book.AuthorId into g
    select new { AuthorId = g.Key, Total = g.Sum(b => b.Price) }
).ToListAsync();
```

```csharp
var statsByGenre = await (
    from book in db.Table<Book>()
    group book by book.Genre into g
    select new
    {
        Genre = g.Key,
        Count = g.Count(),
        AvgPrice = g.Average(b => b.Price),
        MinPrice = g.Min(b => b.Price),
        MaxPrice = g.Max(b => b.Price)
    }
).ToListAsync();
```

## Sum vs Total

`g.Sum(b => b.Price)` emits `COALESCE(SUM(...), 0)`, matching `Enumerable.Sum`. An empty or all-`NULL` input returns `0` and the result keeps the numeric type you projected. The nullable overloads (`Sum(b => (int?)b.X)`) also return `0`, never `null`.

`SQLiteFunctions.Total(g.Select(b => b.Price))` emits SQLite's `total(...)` and always returns a `REAL` (`double`) value, `0.0` for empty input. Reach for it when you want a `double` result regardless of the projected column type.

```csharp
var revenueByAuthor = await (
    from book in db.Table<Book>()
    group book by book.AuthorId into g
    select new
    {
        AuthorId = g.Key,
        Revenue = SQLiteFunctions.Total(g.Select(b => b.Price))
    }
).ToListAsync();
```

At the root of a query, call `Total` directly on the queryable.

```csharp
double revenue = await db.Table<Book>()
    .Where(b => b.AuthorId == 1)
    .TotalAsync(b => b.Price);
```

## Concatenating Strings Across Rows

`string.Join` over an `IQueryable` translates to SQLite's `group_concat` aggregate. The inner query runs as a correlated subquery and the rows are joined into one string.

```csharp
var titlesPerAuthor = await (
    from a in db.Table<Author>()
    select new
    {
        Author = a.Name,
        Titles = string.Join(", ", db.Table<Book>()
            .Where(b => b.AuthorId == a.Id)
            .Select(b => b.Title))
    }
).ToListAsync();
```

### Root-Level StringJoin

For a single SQL roundtrip at the root of a query, call `StringJoin` on the queryable directly.

```csharp
string allTitles = await db.Table<Book>()
    .OrderBy(b => b.Id)
    .Select(b => b.Title)
    .StringJoinAsync(", ");
```

A plain `string.Join(sep, queryable)` at the root, without `StringJoin`, still works but does not get `group_concat`. The runtime enumerates every row first and concatenates them in memory. `StringJoin` runs the aggregation in SQL.

## Filtering Inside an Aggregate

Chain a `Where` clause before any grouping aggregate to restrict which rows the aggregate sees. This translates to SQLite's `FILTER (WHERE ...)` clause, added in SQLite 3.30.

```csharp
var rows = await (
    from book in db.Table<Book>()
    group book by book.AuthorId into g
    select new
    {
        AuthorId = g.Key,
        PriceyRevenue = g.Where(x => x.Price >= 10).Sum(x => x.Price),
        TotalRevenue = g.Sum(x => x.Price)
    }
).ToListAsync();
```


## Filter Groups with HAVING

Add a `where` clause after the `into` to filter groups. This becomes a `HAVING` clause in SQL.

```csharp
var popularGenres = await (
    from book in db.Table<Book>()
    group book by book.Genre into g
    where g.Count() > 5
    select new { Genre = g.Key, Count = g.Count() }
).ToListAsync();
```

## Group by Multiple Columns

Project into an anonymous type to group by more than one column.

```csharp
var grouped = await (
    from book in db.Table<Book>()
    group book by new { book.Genre, book.AuthorId } into g
    select new { g.Key.Genre, g.Key.AuthorId, Count = g.Count() }
).ToListAsync();
```

## Grouping a Join

Group the results of a join to produce aggregates across related tables.

```csharp
var authorStats = await (
    from book in db.Table<Book>()
    join author in db.Table<Author>() on book.AuthorId equals author.Id
    group book by author.Name into g
    select new { Author = g.Key, BookCount = g.Count(), AvgPrice = g.Average(b => b.Price) }
).ToListAsync();
```
