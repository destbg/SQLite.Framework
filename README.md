# SQLite.Framework

A lightweight, experimental ORM for SQLite in .NET MAUI with AOT support and LINQ-style `IQueryable` querying.

## Features

- **AOT-ready**: Designed for Ahead-Of-Time compilation in .NET MAUI apps.
- **IQueryable interface**: Write LINQ queries against your SQLite database.
- **Inspired by EF & sqlite-net-pcl**: Familiar patterns with minimal overhead.

> **⚠️ Experimental**  
> This package is new and not recommended for production use.

## Installation

Install via NuGet:

```bash
dotnet add package SQLite.Framework
```

## Quick Start

1. **Define your model**:

   ```csharp
   public class Person
   {
       [Key, AutoIncrement]
       public int Id { get; set; }
       public required string Name { get; set; }
   }
   ```
   Using the required keyword is optional but recommended for better nullability checks (The framework will automatically make them NOT NULL in the database if the required keyword is used).

2. **Initialize the context**:

   ```csharp
   using Microsoft.Data.Sqlite;
   using SQLite.Framework;

   var connection = new SqliteConnection("Data Source=app.db");
   var context = new SQLiteDatabase(connection);
   context.Table<Person>().CreateTable();
   ```

3. **Query with LINQ**:

   ```csharp
   // Insert
   context.Add(new Person { Name = "Alice" });

   // Query
   var results = context.Table<Person>()
        .Where(p => p.Name.StartsWith("A"))
        .OrderBy(p => p.Id)
        .Select(p => new { p.Id + 1, p.Name })
        .ToList();
   ```

4. **Async query with LINQ**:

   ```csharp
   // Insert
   await context.AddAsync(new Person { Name = "Alice" });

   // Query
   var results = await context.Table<Person>()
        .Select(p => p.Id)
        .ToListAsync();
   ```

## Limitations

- **No `GroupBy` support** yet.
- No transactions either.

## Contributing

This project is in early development. Feel free to:

- Report bugs or missing features.
- Submit PRs to add functionality or tests.

## License

MIT © Nikolay Kostadinov