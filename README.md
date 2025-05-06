# SQLite.Framework

A lightweight, experimental ORM for SQLite, designed for .NET MAUI with AOT support and LINQ-style `IQueryable` querying.

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
       public DateTime? BirthDate { get; set; }
   }
   ```

2. **Initialize the context**:

   ```csharp
   using Microsoft.Data.Sqlite;
   using SQLite.Framework;

   var connection = new SqliteConnection("Data Source=app.db");
   var context = new SQLiteDatabase(connection);
   context.Table<Person>().CreateTable();
   ```

    On the table class, you can use the [Table] attribute to specify the table name.

    On the class properties:
    - The [Column] attribute specifies the column name.
    - The [NotMapped] attribute ignores the property.
    - The [Key] attribute specifies the primary key.
    - The [AutoIncrement] attribute is used to specify that the column should be auto-incremented.
    - The [Required] attribute is used to specify that the column is NOT NULL (columns are NOT NULL by default, but using the ? operator marks them as nullable).

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

## AOT Support

In order to use this library in AOT scenarios, you need to make sure the objects you are querying are either:

- Part of the assembly that is being AOT compiled (in other words it needs to be part of the code you see).
- Or simply make sure the classes are referenced in your code.

## Limitations

- **No `GroupBy` support** yet.

## Contributing

This project is in early development. Feel free to:

- Report bugs or missing features.
- Submit PRs to add functionality or tests.

## License

MIT © Nikolay Kostadinov