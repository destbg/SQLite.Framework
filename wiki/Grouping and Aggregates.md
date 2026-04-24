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

## Materializing Groupings

You can also turn a `GroupBy` straight into a list or dictionary of `IGrouping<TKey, TElement>`:

```csharp
var byAuthor = db.Table<Book>()
    .GroupBy(b => b.AuthorId)
    .ToDictionary(g => g.Key, g => g.ToList());

foreach (IGrouping<int, Book> group in db.Table<Book>().GroupBy(b => b.AuthorId))
{
    Console.WriteLine($"Author {group.Key}");
    foreach (Book book in group)
    {
        Console.WriteLine($"  {book.Title}");
    }
}
```

The SQL asks for every matching row. There is no `GROUP BY` in the SQL. The groups are built in memory after the rows come back. The key selector runs once per row in .NET.

When you install `SQLite.Framework.SourceGenerator`, the generator writes a small method for each key selector shape it can see. Those methods do not use reflection, so this query works even with `DisableReflectionFallback` set. Without the generator, the framework uses reflection at run time to read the key from each row. In strict mode without the generator, the query throws with a clear error.

The generator writes code for the common shapes:

- `b => b.Id`, a simple property access.
- `b => new { b.X, b.Y }`, an anonymous type key.
- `b => b.X > 0`, a simple boolean or numeric check.
- `b => b.X + b.Y`, simple arithmetic.

Shapes the generator cannot write code for include calls to your own methods and casts to types the generator does not know. For those, either change the key to one of the shapes above, or fetch the rows with `ToListAsync()` first and call LINQ `GroupBy` on the result.

Only a direct `GroupBy(keySelector)` call works here. If you need to filter or order the groups, do it after the `ToDictionary` or `ToList`.
