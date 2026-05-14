# SQLite.Framework

LINQ-to-SQL for SQLite, with the LINQ surface of EF Core but without the runtime weight - and without the trimming and AOT pain that EF Core still has on Native AOT today. Built for .NET MAUI, Avalonia, and any other AOT-published .NET 8/9/10 app where you want to use full-featured `IQueryable` instead of hand-written SQL.

[![NuGet](https://img.shields.io/nuget/v/SQLite.Framework.svg)](https://www.nuget.org/packages/SQLite.Framework/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SQLite.Framework.svg)](https://www.nuget.org/packages/SQLite.Framework/)
[![codecov](https://codecov.io/gh/destbg/SQLite.Framework/branch/main/graph/badge.svg)](https://codecov.io/gh/destbg/SQLite.Framework)
[![Tests](https://github.com/destbg/SQLite.Framework/actions/workflows/coverage.yml/badge.svg)](https://github.com/destbg/SQLite.Framework/actions/workflows/coverage.yml)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![GitHub stars](https://img.shields.io/github/stars/destbg/SQLite.Framework.svg?style=social)](https://github.com/destbg/SQLite.Framework/stargazers)

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("library.db").Build();
using var db = new SQLiteDatabase(options);

var authors = await (
    from b in db.Table<Book>()
    join a in db.Table<Author>() on b.AuthorId equals a.Id
    where b.Price > 10
    group b by a.Name into g
    orderby g.Sum(x => x.Price) descending
    select new
    {
        Author = g.Key,
        Books = g.Count(),
        Revenue = g.Sum(x => x.Price)
    }
).Skip(10).Take(20).ToListAsync();
```

That whole expression is one SQL query. The framework keeps the generated SQL close to the shape of the LINQ chain you wrote and does not wrap it in extra subqueries or rewrite it behind your back to make a method work. When a LINQ method does not map cleanly to SQL, you get a clear `NotSupportedException` instead of a silently rewritten query. No client-side fallback that pulls rows into memory, and no reflection at runtime when you opt into the source generator.

## Why this and not...

| You're using | What you'll like here | What you'll lose |
|---|---|---|
| **EF Core** | Same `IQueryable` shape, smaller dependency, AOT works with minimal setup, no migrations or change tracker overhead. | EF's full mapping model (owned types, value converters via fluent API, complex inheritance). |
| **sqlite-net-pcl** | Real LINQ - joins, group-by, subqueries, projections, FTS5, JSON, window functions all translate to SQL. AOT-friendly with the source generator. | Nothing meaningful, the API is similar where it overlaps and the migration is small. |
| **Dapper** | No more raw SQL strings (although you can still call Query and Execute just the same) and type-safe queries. | Multi-database support, Dapper isn't SQLite-specific. |

See the [Migrating from sqlite-net-pcl](https://destbg.github.io/SQLite.Framework/#/Migrating%20from%20sqlite-net-pcl) or [Migrating from EF Core](https://destbg.github.io/SQLite.Framework/#/Migrating%20from%20EF%20Core) page if that's your starting point.

## Performance

Benchmarks against EF Core 10 and sqlite-net-pcl 1.9 live on the [Performance](https://destbg.github.io/SQLite.Framework/#/Performance) docs page.

## Status

The library is exercised at 100% code coverage. It targets .NET 8, 9, and 10.

## Documentation

The full docs live at **[destbg.github.io/SQLite.Framework](https://destbg.github.io/SQLite.Framework)**. Start with the [Overview](https://destbg.github.io/SQLite.Framework/#/Overview) and [Getting Started](https://destbg.github.io/SQLite.Framework/#/Getting%20Started) pages.

The same content is also mirrored on the [GitHub Wiki](https://github.com/destbg/SQLite.Framework/wiki), but GitHub Pages is the primary source for the docs.

## Installation

```bash
dotnet add package SQLite.Framework
```

The provider packages all expose the same API and assembly name, so you can swap between them without touching code:

| Package | Use when |
|---|---|
| `SQLite.Framework` | Default. Uses the SQLite version that ships with the OS. |
| `SQLite.Framework.Bundled` | Ships its own SQLite binary. Use when the OS-bundled SQLite is too old. |
| `SQLite.Framework.Cipher` | Uses SQLCipher for encrypted databases. |
| `SQLite.Framework.Base` | Bring-your-own SQLitePCLRaw provider. |

JSON, JSONB, FTS5, and window functions are built into all four.

## Quick Start

1. **Define your model.**

```csharp
public class Person
{
    [Key, AutoIncrement]
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime? BirthDate { get; set; }

    [ReferencesTable(typeof(Person))]
    public int? ManagerId { get; set; }
}
```

   Per-class attributes: `[Table]`, `[WithoutRowId]`. Per-property: `[Column]`, `[NotMapped]`, `[Key]`, `[Index]`, `[AutoIncrement]`, `[Required]`, `[ReferencesTable]`, `[ForeignKey]`. Columns are NOT NULL by default, use `?` to mark them as nullable.

2. **Open a database.**

```csharp
using SQLite.Framework;

var options = new SQLiteOptionsBuilder("app.db").Build();
using var db = new SQLiteDatabase(options);
db.Schema.CreateTable<Person>();
```

3. **Write LINQ queries.**

```csharp
db.Table<Person>().Add(new Person { Name = "Alice" });

var adults = (
    from p in db.Table<Person>()
    where p.BirthDate < DateTime.Now.AddYears(-18)
    orderby p.Name
    select new { p.Id, p.Name }
).ToList();
```

4. **Async works the same way.**

```csharp
await db.Table<Person>().AddAsync(new Person { Name = "Alice" });

var ids = await (
    from p in db.Table<Person>()
    select p.Id
).ToListAsync();
```

5. **Joins, groupings, projections - all translate to SQL.**

```csharp
var result = await (
    from b in db.Table<Book>()
    join a in db.Table<Author>() on b.AuthorId equals a.Id
    group b by a.Name into g
    select new
    {
        Author = g.Key,
        Count = g.Count()
    }
).ToListAsync();
```

## AOT Support

Install `SQLite.Framework.SourceGenerator` and call `UseGeneratedMaterializers()`:

```bash
dotnet add package SQLite.Framework.SourceGenerator
```

```csharp
using SQLite.Framework.Generated;

var options = new SQLiteOptionsBuilder("app.db")
    .UseGeneratedMaterializers()
    .Build();
```

The generator writes the row-to-object code at build time, so the trimmer keeps every type used in a `Select` and there's no per-query reflection. The `UseGeneratedMaterializers` extension is generated `internal` per project, so each project that builds queries needs its own reference.

Without the generator, the library still runs under AOT but uses reflection for queries. Make sure the model classes are reachable from the entry assembly so the trimmer keeps them. Full setup on the [Source Generator](https://destbg.github.io/SQLite.Framework/#/Source%20Generator) and [Native AOT](https://destbg.github.io/SQLite.Framework/#/Native%20AOT) pages.

## Contributing

Bug reports and missing-feature requests are welcome.

## License

MIT © Nikolay Kostadinov
