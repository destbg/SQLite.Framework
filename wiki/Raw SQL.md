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

The type you pass to `FromSql<T>` does not have to match a table model. You can use any class as long as its property names match the column names the query returns.

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

`FromSql` wraps your SQL in a subquery and generates an outer `SELECT` that references every mapped column on the type. If your raw SQL leaves out a column the type expects, execution throws a `SQLiteException`. The `Message` contains the column name and the `Sql` property holds the full generated SQL:

```csharp
catch (SQLiteException ex)
{
    Console.WriteLine(ex.Message); // no such column: b0.Price
    Console.WriteLine(ex.Sql);     // SELECT b0.Id AS "Id", b0.Title AS "Title", b0.Price AS "Price" ...
}
```

To avoid this, either select all the columns the type needs, [project into a narrower type](#project-into-a-different-type) that only declares the columns your query returns, or use the [direct query methods](#direct-query-methods) which do not wrap your SQL in a subquery and leave unselected properties at their default values.

## Direct Query Methods

For cases where you just want to run SQL and get results back without the subquery wrapping that `FromSql` adds, you can use the `Query` family of methods. Write SQL, pass parameters, get typed results.

### Parameters

Pass parameters as an anonymous object:

```csharp
var books = db.Query<BookSummary>(
    "SELECT BookTitle AS Title, BookPrice AS Price FROM Books WHERE BookPrice < @price",
    new { price = 30.0 }
);
```

Or pass explicit `SQLiteParameter` objects:

```csharp
var books = db.Query<BookSummary>(
    "SELECT BookTitle AS Title, BookPrice AS Price FROM Books WHERE BookAuthorId = @authorId AND BookPrice < @price",
    new SQLiteParameter { Name = "@authorId", Value = 5 },
    new SQLiteParameter { Name = "@price", Value = 30.0 }
);
```

### Column Mapping

`Query<T>` maps result columns to properties by name. If the database column name is different from the property name, alias it in the SQL:

```csharp
// BookTitle is the column name, Title is the property name
var books = db.Query<BookSummary>(
    "SELECT BookTitle AS Title, BookPrice AS Price FROM Books"
);
```

`FromSql` generates these aliases automatically. `Query` does not.

### Available Methods

| Method | Returns |
|---|---|
| `Query<T>(sql, params)` | `List<T>` |
| `QueryFirst<T>(sql, params)` | `T`, throws if no rows |
| `QueryFirstOrDefault<T>(sql, params)` | `T?`, null if no rows |
| `QuerySingle<T>(sql, params)` | `T`, throws if no rows or more than one row |
| `QuerySingleOrDefault<T>(sql, params)` | `T?`, null if no rows, throws if more than one row |
| `ExecuteScalar<T>(sql, params)` | First column of the first row, null if no rows |
| `Execute(sql, params)` | Number of rows affected |

All methods have async versions: `QueryAsync`, `QueryFirstAsync`, `QueryFirstOrDefaultAsync`, `QuerySingleAsync`, `QuerySingleOrDefaultAsync`, `ExecuteScalarAsync`, `ExecuteAsync`.

```csharp
int count = db.ExecuteScalar<int>("SELECT COUNT(*) FROM Books")!;

int affected = await db.ExecuteAsync(
    "DELETE FROM Books WHERE BookAuthorId = @authorId",
    new { authorId = 5 }
);
```

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
