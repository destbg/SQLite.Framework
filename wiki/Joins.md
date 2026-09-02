# Joins

Joins use LINQ query syntax. There are no navigation properties, so you write the join explicitly each time.

## Inner Join

Returns only rows that have a match in both tables.

```csharp
var results = await (
    from book in db.Table<Book>()
    join author in db.Table<Author>() on book.AuthorId equals author.Id
    select new { book.Title, author.Name, book.Price }
).ToListAsync();
```

You can also use the fluent `Join` method:

```csharp
var results = await db.Table<Book>()
    .Join(
        db.Table<Author>(),
        book => book.AuthorId,
        author => author.Id,
        (book, author) => new { book.Title, author.Name, book.Price }
    )
    .ToListAsync();
```

## Left Join

Returns all rows from the left table and matching rows from the right. Where there is no match, the right side values will be `null`.

```csharp
var results = await (
    from author in db.Table<Author>()
    join book in db.Table<Book>() on author.Id equals book.AuthorId into bookGroup
    from book in bookGroup.DefaultIfEmpty()
    select new { author.Name, book.Title }
).ToListAsync();
```

In this group-join form, the second `from` must be directly after the `join`.

## Group Join Aggregates

You can keep the group and aggregate it instead of flattening it. `Count` and `LongCount` translate directly, and their projected values can be aggregated later.

```csharp
var counts = await db.Table<Author>()
    .GroupJoin(
        db.Table<Book>(),
        author => author.Id,
        book => book.AuthorId,
        (author, books) => new { author.Name, BookCount = books.Count() })
    .ToListAsync();
```

## Full Outer Join

Returns matched rows plus the unmatched rows from both sides. .NET has no built-in operator, so use the framework's `FullOuterJoin` method. The result selector gets the outer row (null when only an inner row matched) and the inner row (null when only an outer row matched). Requires SQLite 3.39 or newer.

```csharp
var results = await db.Table<Author>()
    .FullOuterJoin(
        db.Table<Book>(),
        author => author.Id,
        book => book.AuthorId,
        (author, book) => new { Author = author == null ? null : author.Name, Book = book == null ? null : book.Title })
    .ToListAsync();
```

## Cross Join

Returns every combination of rows from both tables.

```csharp
var results = await (
    from author in db.Table<Author>()
    from book in db.Table<Book>()
    select new { author.Name, book.Title }
).ToListAsync();
```

## Correlated SelectMany

A filtered table can be used as the second `from` source. A normal filtered source becomes an inner join. Adding `DefaultIfEmpty()` makes it a left join and returns `null` for a missing row.

```csharp
var matches = await (
    from author in db.Table<Author>()
    from book in db.Table<Book>()
        .Where(book => book.AuthorId == author.Id && book.Price > 0)
        .DefaultIfEmpty()
    select new { author.Name, BookTitle = book == null ? null : book.Title }
).ToListAsync();
```

The correlated filter must be translatable to SQL.

## Multiple Joins

Chain as many joins as you need:

```csharp
var results = await (
    from book in db.Table<Book>()
    join author in db.Table<Author>() on book.AuthorId equals author.Id
    join publisher in db.Table<Publisher>() on book.PublisherId equals publisher.Id
    select new { book.Title, author.Name, publisher.CompanyName }
).ToListAsync();
```

You can also mix inner and left joins:

```csharp
var results = await (
    from book in db.Table<Book>()
    join author in db.Table<Author>() on book.AuthorId equals author.Id
    join review in db.Table<Review>() on book.Id equals review.BookId into reviewGroup
    from review in reviewGroup.DefaultIfEmpty()
    select new { book.Title, author.Name, review.Rating }
).ToListAsync();
```

## Filtering a Join

Add a `where` clause after the join:

```csharp
var results = await (
    from book in db.Table<Book>()
    join author in db.Table<Author>() on book.AuthorId equals author.Id
    where author.Country == "USA" && book.Price < 30
    select new { book.Title, author.Name }
).ToListAsync();
```
