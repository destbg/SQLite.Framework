# SQLite.Framework

LINQ-to-SQL for SQLite, with the LINQ surface of EF Core but without the runtime weight - and without the trimming and AOT pain that EF Core still has on Native AOT today. Built for .NET MAUI, Avalonia, and any other AOT-published .NET 8/9/10 app where you want to use full-featured `IQueryable` instead of hand-written SQL.

[![NuGet](https://img.shields.io/nuget/v/SQLite.Framework.svg)](https://www.nuget.org/packages/SQLite.Framework/)
[![codecov](https://codecov.io/gh/destbg/SQLite.Framework/branch/main/graph/badge.svg)](https://codecov.io/gh/destbg/SQLite.Framework)

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

That whole expression is one SQL query. No client-side fallback, no reflection at runtime when you opt into the source generator.

## Why this and not...

| You're using | What you'll like here | What you'll lose |
|---|---|---|
| **EF Core** | Same `IQueryable` shape, smaller dependency, AOT works with minimal setup, no migrations or change tracker overhead. | EF's full mapping model (owned types, value converters via fluent API, complex inheritance). |
| **sqlite-net-pcl** | Real LINQ - joins, group-by, subqueries, projections, FTS5, JSON, window functions all translate to SQL. AOT-friendly with the source generator. | Nothing meaningful; the API is similar where it overlaps and the migration is small. |
| **Dapper** | No more raw SQL strings (although you can still call Query and Execute just the same) and type-safe queries. | Multi-database support; Dapper isn't SQLite-specific. |

See the [Migrating from sqlite-net-pcl](https://destbg.github.io/SQLite.Framework/#/Migrating%20from%20sqlite-net-pcl) page if that's your starting point.

## Performance

Head-to-head against EF Core 10 and sqlite-net-pcl 1.9 on the same in-process SQLite file. 100 rows per operation, .NET 10, BenchmarkDotNet. Lower is better.

**Read 100 rows into a `List<Book>`:**

| ORM | Mean | Allocated | Ratio |
|---|---:|---:|---:|
| **SQLite.Framework + SourceGenerator** | **38.7 μs** | **23.3 KB** | **0.41** |
| sqlite-net-pcl | 43.3 μs | 15.4 KB | 0.46 |
| EF Core 10 (`AsNoTracking`) | 72.1 μs | 47.6 KB | 0.76 |
| SQLite.Framework (reflection path) | 94.9 μs | 69.4 KB | 1.00 |

**Bulk insert 100 rows (single transaction):**

| ORM | Mean | Allocated | Ratio |
|---|---:|---:|---:|
| **SQLite.Framework + SourceGenerator** | **114.6 μs** | **4.1 KB** | **0.85** |
| sqlite-net-pcl (`InsertAll`) | 139.0 μs | 20.6 KB | 1.03 |
| EF Core 10 (`AddRange` + `SaveChanges`) | 2,095 μs | 914.9 KB | 15.52 |

**Bulk update 100 rows by predicate:**

| ORM | Mean | Allocated | Ratio |
|---|---:|---:|---:|
| **SQLite.Framework (`ExecuteUpdate`)** | **142.5 μs** | **14.9 KB** | **1.00** |
| EF Core 10 (`ExecuteUpdate`) | 164.6 μs | 16.3 KB | 1.16 |
| sqlite-net-pcl (`UpdateAll`) | 419.0 μs | 198.3 KB | 2.94 |

**Join + project (1000 Books and 50 Authors, filter `Price > 50`, sort, project to a DTO):**

| ORM | Mean | Allocated | Ratio |
|---|---:|---:|---:|
| **SQLite.Framework + SourceGenerator** | **75.2 μs** | **36.7 KB** | **0.66** |
| EF Core 10 | 95.3 μs | 61.9 KB | 0.84 |
| SQLite.Framework (reflection path) | 115.1 μs | 55.8 KB | 1.00 |
| sqlite-net-pcl | 404.1 μs | 155.4 KB | 3.54 |

sqlite-net-pcl's `TableQuery<T>` is `IEnumerable<T>`, not `IQueryable<T>`, so the LINQ join binds to `Enumerable.Join`. The whole `Books` and `Authors` tables load into memory before the filter and join run client-side.

## Status

The library is exercised by 9000+ test cases across all suites at 100% line coverage; the main test project alone runs 1800+ tests. The library targets .NET 8, 9, and 10. SemVer is followed for breaking changes.

## Documentation

- [GitHub Wiki](https://github.com/destbg/SQLite.Framework/wiki)
- [GitHub Pages](https://destbg.github.io/SQLite.Framework)

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
}
```

   Per-class attributes: `[Table]`, `[WithoutRowId]`. Per-property: `[Column]`, `[NotMapped]`, `[Key]`, `[Index]`, `[AutoIncrement]`, `[Required]`. Columns are NOT NULL by default; use `?` to mark them as nullable.

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
