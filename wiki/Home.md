# SQLite.Framework

A lightweight [ORM](https://en.wikipedia.org/wiki/Object%E2%80%93relational_mapping) for SQLite, built for .NET. It gives you LINQ queries, async support, and AOT compatibility, with an API that will feel familiar if you have used Entity Framework before.

```csharp
using SQLiteDatabase db = new("library.db");

var books = db.Table<Book>();
await books.CreateTableAsync();

await books.AddAsync(new Book { Title = "Clean Code", Price = 29.99m });

var affordable = await books.Where(b => b.Price < 30).ToListAsync();
```

## Packages

| Package | When to use |
|---|---|
| `SQLite.Framework` | Default. Uses the SQLite version that ships with the OS. Works on all major platforms. |
| `SQLite.Framework.Bundled` | Ships its own SQLite binary. Use this when the OS-provided SQLite is too old or you need a specific version. |
| `SQLite.Framework.Cipher` | Uses SQLCipher for encrypted databases. Set the `Key` property before the first operation to enable encryption. |
| `SQLite.Framework.Base` | No SQLite provider included. Use this when you want to supply your own SQLitePCLRaw provider. You are responsible for calling `SQLitePCL.Batteries_V2.Init()` before creating a database. |

All packages expose the same API and assembly name, so you can swap between them without changing any code.

## Features

- LINQ queries with `IQueryable` support
- Async versions of every operation
- CRUD operations with typed tables
- Joins, group by, aggregates, and subqueries
- Bulk delete and update with `ExecuteDelete` and `ExecuteUpdate`
- Transactions using SQLite savepoints
- Raw SQL via `FromSql`
- AOT compatible, works great in .NET MAUI apps
- Supports .NET 8, 9, and 10

## Pages

- [Getting Started](Getting%20Started)
- [Defining Models](Defining%20Models)
- [CRUD Operations](CRUD%20Operations)
- [Querying](Querying)
- [Expressions](Expressions)
- [Subqueries](Subqueries)
- [Joins](Joins)
- [Grouping and Aggregates](Grouping%20and%20Aggregates)
- [Bulk Operations](Bulk%20Operations)
- [Transactions](Transactions)
- [Multi-threading](Multi-threading)
- [Raw SQL](Raw%20SQL)
- [Common Table Expressions](Common%20Table%20Expressions)
- [Data Types](Data%20Types)
- [Storage Options](Storage%20Options)
- [Custom Converters](Custom%20Converters)
- [JSON and JSONB](JSON%20and%20JSONB)
- [Performance](Performance)
- [Native AOT](Native%20AOT)
- [Migrating from sqlite-net-pcl](Migrating%20from%20sqlite-net-pcl)
