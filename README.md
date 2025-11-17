# SQLite.Framework

A lightweight ORM for SQLite, designed for .NET MAUI with AOT support and LINQ-style `IQueryable` querying.

[![NuGet](https://img.shields.io/nuget/v/SQLite.Framework.svg)](https://www.nuget.org/packages/SQLite.Framework/)

## Features

- **AOT-ready**: Designed for Ahead-Of-Time compilation in .NET MAUI apps.
- **IQueryable interface**: Write LINQ queries against your SQLite database.
- **Inspired by EF & sqlite-net-pcl**: Familiar patterns with minimal overhead.

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
   using SQLite.Framework;

   var context = new SQLiteDatabase("app.db");
   context.Table<Person>().CreateTable();
   ```

    On the table class, you can use the following:
    - The [Table] attribute to specify the table name.
    - The [WithoutRowId] attribute to use the [without rowid](https://sqlite.org/withoutrowid.html) optimization.

    On the class properties:
    - The [Column] attribute specifies the column name.
    - The [NotMapped] attribute ignores the property.
    - The [Key] attribute specifies the primary key.
    - The [Index] attribute creates an index on the column or make a column unique.
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

- Part of the assembly that is being AOT compiled (the user's code).
- Or simply make sure the classes are referenced in your code.

## Contributing

Feel free to:

- Report bugs or missing features.
- Submit PRs to add functionality or tests.

## License

MIT © Nikolay Kostadinov
