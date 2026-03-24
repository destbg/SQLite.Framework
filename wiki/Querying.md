# Querying

`db.Table<T>()` returns an `IQueryable<T>`. You can chain standard LINQ methods on it and call one of the terminal methods to run the query.

## Get All Records

```csharp
var books = await db.Table<Book>().ToListAsync();
```

## Filter with Where

```csharp
var cheap = await db.Table<Book>()
    .Where(b => b.Price < 20)
    .ToListAsync();

var byAuthor = await db.Table<Book>()
    .Where(b => b.AuthorId == 3 && b.InStock)
    .ToListAsync();
```

## Project with Select

```csharp
var titles = await db.Table<Book>()
    .Select(b => b.Title)
    .ToListAsync();

var summaries = await db.Table<Book>()
    .Select(b => new { b.Title, b.Price })
    .ToListAsync();
```

You can project into a named type as well:

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

## Sort

```csharp
var sorted = await db.Table<Book>()
    .OrderBy(b => b.Price)
    .ToListAsync();

var sortedDesc = await db.Table<Book>()
    .OrderByDescending(b => b.PublishedAt)
    .ToListAsync();
```

Chain `ThenBy` or `ThenByDescending` for multiple sort columns:

```csharp
var sorted = await db.Table<Book>()
    .OrderBy(b => b.Genre)
    .ThenByDescending(b => b.Price)
    .ToListAsync();
```

## Pagination

```csharp
int page = 2;
int pageSize = 10;

var results = await db.Table<Book>()
    .OrderBy(b => b.Title)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

## Distinct

```csharp
var genres = await db.Table<Book>()
    .Select(b => b.Genre)
    .Distinct()
    .ToListAsync();
```

## Get a Single Record

```csharp
// Throws if no match
var book = await db.Table<Book>().FirstAsync(b => b.Id == 1);

// Returns null if no match
var book = await db.Table<Book>().FirstOrDefaultAsync(b => b.Id == 1);

// Throws if more than one match
var book = await db.Table<Book>().SingleAsync(b => b.Isbn == "978-0132350884");

// Returns null if no match, throws if more than one
var book = await db.Table<Book>().SingleOrDefaultAsync(b => b.Isbn == "978-0132350884");
```

## Count

```csharp
int total = await db.Table<Book>().CountAsync();

int cheapCount = await db.Table<Book>().CountAsync(b => b.Price < 20);
```

## Any and All

```csharp
bool hasBooks = await db.Table<Book>().AnyAsync();

bool hasExpensive = await db.Table<Book>().AnyAsync(b => b.Price > 100);

bool allInStock = await db.Table<Book>().AllAsync(b => b.InStock);
```

## Contains

```csharp
var ids = new List<int> { 1, 2, 3 };

var books = await db.Table<Book>()
    .Where(b => ids.Contains(b.Id))
    .ToListAsync();
```

## Other Collection Types

```csharp
var array = await db.Table<Book>().ToArrayAsync();

var set = await db.Table<Book>()
    .Select(b => b.Genre)
    .ToHashSetAsync();

var dict = await db.Table<Book>()
    .ToDictionaryAsync(b => b.Id, b => b.Title);
```

## Chaining

All of these methods can be chained together freely:

```csharp
var results = await db.Table<Book>()
    .Where(b => b.InStock && b.Price < 50)
    .OrderBy(b => b.Title)
    .Skip(1)
    .Take(5)
    .Select(b => new { b.Title, b.Price })
    .ToListAsync();
```
