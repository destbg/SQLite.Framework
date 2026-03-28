# Raw SQL

`FromSql` lets you write a SQL query by hand and get back a typed result. Use it when the LINQ API does not cover what you need.

## Basic Query

```csharp
var books = await db.FromSql<Book>("SELECT * FROM Books")
    .ToListAsync();
```

## With Parameters

Always use parameters instead of string concatenation to avoid SQL injection.

```csharp
var books = await db.FromSql<Book>(
    "SELECT * FROM Books WHERE BookPrice < @price AND BookInStock = 1",
    new SQLiteParameter { Name = "@price", Value = 30.0m }
).ToListAsync();
```

Pass multiple parameters as additional arguments:

```csharp
var books = await db.FromSql<Book>(
    "SELECT * FROM Books WHERE BookGenre = @genre AND BookAuthorId = @authorId",
    new SQLiteParameter { Name = "@genre", Value = "Fiction" },
    new SQLiteParameter { Name = "@authorId", Value = 5 }
).ToListAsync();
```

## Project Into a Different Type

The type you pass to `FromSql<T>` does not have to match a table model. You can use any class or anonymous-like type as long as its property names match the column names returned by the query.

```csharp
public class BookSummary
{
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
}

var summaries = await db.FromSql<BookSummary>(
    "SELECT BookTitle AS Title, BookPrice AS Price FROM Books"
).ToListAsync();
```

## Chain LINQ After FromSql

You can chain `Where`, `OrderBy`, `Take`, and other LINQ methods after `FromSql`. They run as part of the same query.

```csharp
var results = await db.FromSql<Book>("SELECT * FROM Books")
    .Where(b => b.Price < 30)
    .OrderBy(b => b.Title)
    .Take(10)
    .ToListAsync();
```

## FromSql on a Table

`SQLiteTable<T>` also has a `FromSql` method that works the same way:

```csharp
var books = db.Table<Book>();

var results = await books.FromSql(
    "SELECT * FROM Books WHERE BookInStock = 1"
).ToListAsync();
```

## Missing Columns

`FromSql` wraps your SQL in a subquery and generates an outer `SELECT` that references every mapped column on the type. If your raw SQL omits a column that the type expects, execution throws a `SQLiteException`. The `Message` contains the offending column name and the `Sql` property holds the full generated SQL:

```csharp
catch (SQLiteException ex)
{
    Console.WriteLine(ex.Message); // no such column: b0.Price
    Console.WriteLine(ex.Sql);     // SELECT b0.Id AS "Id", b0.Title AS "Title", b0.Price AS "Price" ...
}
```

To avoid this, either select all columns the type needs or [project into a narrower type](#project-into-a-different-type) that only declares the columns your query returns.

## Inspecting Generated SQL

Use `ToSql()` to see what SQL a LINQ query produces. Useful for debugging.

```csharp
string sql = db.Table<Book>()
    .Where(b => b.Price < 30)
    .OrderBy(b => b.Title)
    .ToSql();

Console.WriteLine(sql);
```

Use `ToSqlCommand()` to get the command with both the SQL and the bound parameters:

```csharp
SQLiteCommand cmd = db.Table<Book>()
    .Where(b => b.Price < 30)
    .ToSqlCommand();

Console.WriteLine(cmd.CommandText);

foreach (var p in cmd.Parameters)
    Console.WriteLine($"{p.Name} = {p.Value}");
```
