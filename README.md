# SQLite.Framework

A lightweight ORM for SQLite, designed for .NET MAUI and Avalonia with AOT support and LINQ-style `IQueryable` querying.

[![NuGet](https://img.shields.io/nuget/v/SQLite.Framework.svg)](https://www.nuget.org/packages/SQLite.Framework/)

## Features

- **AOT-ready**: Designed for Ahead-Of-Time compilation in .NET MAUI and Avalonia apps.
- **IQueryable interface**: Write LINQ queries against your SQLite database.
- **Inspired by EF & sqlite-net-pcl**: Familiar patterns with minimal overhead.

## Documentation

- [GitHub Wiki](https://github.com/destbg/SQLite.Framework/wiki) - browse the docs on GitHub
- [GitHub Pages](https://destbg.github.io/SQLite.Framework) - the same docs as a standalone site

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

   var options = new SQLiteOptionsBuilder("app.db").Build();
   using var context = new SQLiteDatabase(options);
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

For Native AOT builds, install `SQLite.Framework.SourceGenerator` and turn it on when you build your options:

```bash
dotnet add package SQLite.Framework.SourceGenerator
```

```csharp
using SQLite.Framework.Generated;

var options = new SQLiteOptionsBuilder("app.db")
    .UseGeneratedMaterializers()
    .Build();
```

The generator writes the code that reads SQLite rows into your .NET objects at build time, so the trimmer can see every public type used in a `Select` and no reflection is needed for those. Private types and private methods that appear in a `Select` still go through a small amount of reflection.

`UseGeneratedMaterializers` is generated per project. The class and the extension method are marked `internal`, so if your solution has several projects that build LINQ queries, each one needs its own reference to `SQLite.Framework.SourceGenerator` and its own call to `UseGeneratedMaterializers`.

See the [Source Generator](https://github.com/destbg/SQLite.Framework/wiki/Source-Generator) and [Native AOT](https://github.com/destbg/SQLite.Framework/wiki/Native-AOT) pages for the full setup.

Without the generator, the library still runs under AOT but uses reflection for each query. In that case, make sure the classes you query are either part of the AOT-compiled assembly or referenced directly in your code so the trimmer keeps them.

## Contributing

Feel free to:

- Report bugs or missing features.
- Submit PRs to add functionality or tests.

## License

MIT © Nikolay Kostadinov
