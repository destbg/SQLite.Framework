# Subqueries

Any `IQueryable<T>` can be used inside a `Where` clause as a subquery. It runs as a single SQL query with no extra round-trips to the database.

## IN with a Subquery

Use `.Contains` on a subquery to produce `IN (SELECT ...)`:

```csharp
var authorIds = db.Table<Book>()
    .Where(b => b.Price < 10)
    .Select(b => b.AuthorId);

var books = await db.Table<Book>()
    .Where(b => authorIds.Contains(b.AuthorId))
    .ToListAsync();
```

```sql
SELECT ...
FROM "Books" AS b0
WHERE b0.BookAuthorId IN (
    SELECT b1.BookAuthorId AS "AuthorId"
    FROM "Books" AS b1
    WHERE b1.BookPrice < @p0
)
```

The same thing works with query syntax:

```csharp
var books = await (
    from book in db.Table<Book>()
    where (
        from b in db.Table<Book>()
        where b.Price < 10
        select b.AuthorId
    ).Contains(book.AuthorId)
    select book
).ToListAsync();
```

## Correlated Subqueries

The inner query can reference columns from the outer query:

```csharp
var books = await (
    from book in db.Table<Book>()
    where (
        from b in db.Table<Book>()
        where b.AuthorId == book.AuthorId && b.Price < 10
        select b.Id
    ).Contains(book.Id)
    select book
).ToListAsync();
```

```sql
SELECT ...
FROM "Books" AS b0
WHERE b0.BookId IN (
    SELECT b1.BookId AS "Id"
    FROM "Books" AS b1
    WHERE b1.BookAuthorId = b0.BookAuthorId AND b1.BookPrice < @p0
)
```

## Scalar Subqueries

Call an aggregate method on a subquery to compare against a single value:

```csharp
var mostExpensive = await db.Table<Book>()
    .Where(b => b.Price == db.Table<Book>().Max(b2 => b2.Price))
    .ToListAsync();
```

Query syntax with `.Max()` or `.Min()` on an inner query also works:

```csharp
var books = await (
    from book in db.Table<Book>()
    where book.Id == (
        from b in db.Table<Book>()
        where b.Title == "Clean Code"
        select b.Id
    ).Max()
    select book
).ToListAsync();
```

```sql
SELECT ...
FROM "Books" AS b0
WHERE b0.BookId = (
    SELECT MAX(b1.BookId)
    FROM "Books" AS b1
    WHERE b1.BookTitle = @p0
)
```
