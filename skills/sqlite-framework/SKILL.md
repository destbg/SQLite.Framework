---
name: sqlite-framework
description: Write and troubleshoot C# data access with SQLite.Framework, including provider selection, models, LINQ queries, writes, migrations, dependency injection, and Native AOT materializers. Use when a project references SQLite.Framework or its Base, Bundled, Cipher, DependencyInjection, or SourceGenerator packages.
license: MIT
---

# SQLite.Framework

Use the APIs available in the installed package version. SQLite.Framework has an `IQueryable` surface, but no EF Core change tracker, navigation loading, `DbContext`, or `SaveChanges`. Writes execute immediately.

## Choose packages

Use exactly one runtime provider package; they expose the same `SQLite.Framework` assembly:

- `SQLite.Framework`: OS SQLite on desktop and iOS; bundled SQLite on Android.
- `SQLite.Framework.Bundled`: bundled SQLite on every supported platform.
- `SQLite.Framework.Cipher`: SQLCipher; configure `UseEncryptionKey` before opening the database.
- `SQLite.Framework.Base`: supply and initialize a SQLitePCLRaw provider before opening a database.

Add `SQLite.Framework.DependencyInjection` for service registration and `SQLite.Framework.SourceGenerator` for generated materializers. Keep the SQLite.Framework package versions aligned. On the default and Base providers, declare the supported SQLite floor with `UseMinimumSqliteVersion`; Bundled and Cipher do not expose the same version enum values. An API being available at compile time does not guarantee the loaded SQLite supports it.

## Models, schema, and queries

Include the extension namespace explicitly. Standard mapping attributes come from `System.ComponentModel.DataAnnotations` and `System.ComponentModel.DataAnnotations.Schema`; SQLite-specific attributes come from `SQLite.Framework.Attributes`.

```csharp
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;

using var db = new SQLiteDatabase(new SQLiteOptionsBuilder("app.db").Build());
await db.Schema.CreateTableAsync<Person>();
await db.Table<Person>().AddAsync(new Person { Name = "Alice" });

var people = await db.Table<Person>()
    .Where(p => p.Name.StartsWith("A"))
    .OrderBy(p => p.Name)
    .Select(p => new { p.Id, p.Name })
    .ToListAsync();

public class Person
{
    [Key, AutoIncrement]
    public int Id { get; set; }
    public required string Name { get; set; }
    public int? ManagerId { get; set; }
}
```

- Schema operations live on `db.Schema`, not directly on `db`.
- `[Key]` and `[AutoIncrement]` are separate attributes. Auto-increment requires a single integer primary key. By default, `Add` replaces an explicit auto-increment key with the generated key.
- Columns are NOT NULL by default; use nullable properties for nullable columns. Keep navigation properties out of mapping, using `[NotMapped]` where needed; load related rows with explicit queries or joins.
- Compose filters, joins, ordering, and projections before materializing. Do not move a query to LINQ-to-Objects merely to hide a translation error. Unsupported query operators can throw `NotSupportedException`; use the documented SQL-compatible equivalent.
- `UpdateAsync(item)` and `RemoveAsync(item)` operate by primary key. Changing a loaded object does not persist it automatically.
- `AddOrUpdate` uses `INSERT OR REPLACE`, which deletes the conflicting row before inserting and can trigger cascading deletes. Use `Update` or `Upsert` with `DoUpdate` when the existing row must remain in place.

## Migrations and transactions

Creating a table does not migrate an existing table. Add monotonically increasing versions to the migration runner; do not rewrite versions already deployed:

```csharp
await db.Schema.Migrations()
    .Version(1, m => m.CreateTable<Person>())
    .Version(2, m => m.TableChanged<Person>())
    .MigrateAsync();
```

Use explicit rename, fill, and conversion steps when a model change needs to preserve existing data. Changing a storage option does not re-encode existing rows.

Use `using` / `await using` for transactions and commit explicitly; disposal without commit rolls back. Nested transactions use savepoints. A `SQLiteTransaction` belongs to the async flow that opened it; do not pass it between independent flows. Inside an outer transaction, bulk writes can use `runInTransaction: false` to avoid another savepoint.

## Raw SQL

SQLite.Framework has very advanced `IQueryable` support, so most queries should not need raw SQL. Prefer LINQ and make a thorough effort to get the query working before falling back: consult the query documentation, investigate translation errors, and try equivalent supported query shapes. Use raw SQL when the user requests it or you have confirmed that the supported query APIs cannot express the requirement.

Bind values with parameters rather than concatenating or interpolating input into SQL:

```csharp
var rows = await db.QueryAsync<Person>(
    "SELECT Id, Name, ManagerId FROM Person WHERE Name = @name",
    new { name = "Alice" });
```

`FromSql<T>` is composable and wraps the SQL in a subquery that expects every mapped column. Use a narrower DTO or direct `Query<T>` / `QueryAsync<T>` for partial results. Query methods accept one statement; use `Execute` / `ExecuteAsync` for multi-statement batches.

## Dependency injection and Native AOT

- Import `SQLite.Framework.DependencyInjection` and use `services.AddSQLiteDatabase<AppDatabase>(b => b.DatabasePath = dbPath)`. The default lifetime is singleton. A custom database constructor accepts `SQLiteOptions` and passes it to `base(options)`.
- Register `AddSQLiteDatabaseFactory<AppDatabase>` when callers need independently owned database instances, and dispose each created instance.
- For Native AOT, install `SQLite.Framework.SourceGenerator`, import `SQLite.Framework.Generated`, and call `UseGeneratedMaterializers()` on the options builder. The generated extension is internal to each project. Each project that builds queries needs the generator and must contribute its generated registrations to the builder used by those queries.
- Exercise representative queries with `UseGeneratedMaterializers().DisableReflectionFallback()` to detect missing generated coverage. Keep named projection types and referenced members accessible to generated code. Test the published AOT executable as well as a normal build.

## More detail

Read only the guide needed for the current task: [Overview](https://sqlite-framework.net/Docs/Overview), [Schema](https://sqlite-framework.net/Docs/Schema), [Querying](https://sqlite-framework.net/Docs/Querying), [Dependency Injection](https://sqlite-framework.net/Docs/Dependency-Injection), [Source Generator](https://sqlite-framework.net/Docs/Source-Generator), [Native AOT](https://sqlite-framework.net/Docs/Native-AOT), or [Limitations](https://sqlite-framework.net/Docs/Limitations). SQLite decimal arithmetic uses floating-point precision, and `DateTimeOffset` storage does not retain the offset; check the data type rules before relying on exact round trips.
