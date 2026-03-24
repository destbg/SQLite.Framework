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
