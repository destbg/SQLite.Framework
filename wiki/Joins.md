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

The "from" must be always right after the join.

## Cross Join

Returns every combination of rows from both tables.

```csharp
var results = await (
    from author in db.Table<Author>()
    from book in db.Table<Book>()
    select new { author.Name, book.Title }
).ToListAsync();
```

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
