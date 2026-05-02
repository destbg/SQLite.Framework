# SQLite.Framework — AI Quick Reference

A compact reference for an AI assistant working with SQLite.Framework in this project.

## What it is

A LINQ-to-SQLite ORM for .NET. Familiar if you know EF Core: `db.Table<T>()` returns a `SQLiteTable<T>` (which implements `IQueryable<T>`, so any LINQ method works on it). AOT compatible. Async versions of every operation (drop the `Async` suffix for sync).

## Packages

- `SQLite.Framework` — default; uses the OS-bundled SQLite.
- `SQLite.Framework.Bundled` — ships its own SQLite binary.
- `SQLite.Framework.Cipher` — SQLCipher for encrypted databases.
- `SQLite.Framework.Base` — bring your own `SQLitePCLRaw` provider.
- `SQLite.Framework.DependencyInjection` — `AddSQLiteDatabase` for `IServiceCollection`.
- `SQLite.Framework.SourceGenerator` — build-time materializers; required for AOT.

The first four expose the same API and assembly name; swap freely. The version of the added packages must all match.

## Setup

Console / minimal:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .UseWalMode()                     // Ask user if he wants concurrent writes
    .UseGeneratedMaterializers()      // requires SQLite.Framework.SourceGenerator
    .DisableReflectionFallback()      // throw if any query needs runtime reflection
    .Build();

using SQLiteDatabase db = new(options);
await db.Schema.CreateTableAsync<Project>();
```

DI (Microsoft.Extensions.DependencyInjection):

```csharp
services.AddSQLiteDatabase<AppDatabase>(b =>
{
    b.DatabasePath = dbPath;
    b.UseWalMode()
        .UseGeneratedMaterializers()
        .DisableReflectionFallback();
});
```

The default lifetime is `Singleton` (a good fit for desktop/mobile apps). Subclass `SQLiteDatabase` to expose typed tables and centralise schema creation. Use the async schema methods so the UI thread does not block on disk I/O at startup:

```csharp
public class AppDatabase : SQLiteDatabase
{
    public AppDatabase(SQLiteOptions options) : base(options) { }

    public SQLiteTable<Project> Projects => Table<Project>();
    public SQLiteTable<ProjectTask> Tasks => Table<ProjectTask>();

    public async Task InitializeAsync()
    {
        await Schema.CreateTableAsync<Project>();
        await Schema.CreateTableAsync<ProjectTask>();
    }
}

// At app startup, after the service provider is built:
await services.GetRequiredService<AppDatabase>().InitializeAsync();
```

## Defining models

Plain class. Attributes from `System.ComponentModel.DataAnnotations`, `...Schema`, and `SQLite.Framework.Attributes`.

```csharp
[Table("Project")]
public class Project
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [Indexed]
    public required int CategoryId { get; set; }
}
```

Keep entities to the columns of the table. There are no navigation properties — to load related rows, query them explicitly or build a DTO via a `Select` / join projection.

- `[Key]` + `[AutoIncrement]` — SQLite assigns the id and writes it back to the entity after `AddAsync`.
- `[Required]` — NOT NULL. Nullable reference types (`string?`) map automatically.
- `[Indexed]` — single, unique (`IsUnique = true`), named, or composite (same `Name`, different `Order`).
- `[Column("…")]`, `[Table("…")]` — override names.
- `[WithoutRowId]` — class-level; primary key must not be `[AutoIncrement]`.
- `[NotMapped]` exists for the rare case you need a non-column member on the class (a derived value used by validation, etc.), but prefer keeping such logic outside the entity.

Schema setup is idempotent (`CREATE TABLE IF NOT EXISTS`), so calling `Schema.CreateTable<T>()` on every startup is safe. Track migrations through `db.Pragmas.UserVersion`.

## Data types

- INTEGER: `int`, `long`, `short`, `byte`, `bool`, `enum`, `DateTime` / `DateOnly` / `TimeOnly` / `TimeSpan` / `DateTimeOffset` (all stored as ticks).
- REAL: `float`, `double`, `decimal` (decimal stored via `double`, ~15-16 digits precision).
- TEXT: `string`, `char`, `Guid` (lowercase hyphenated).
- BLOB: `byte[]`.

All work as nullable. `DateTimeOffset` does not preserve offset — store offset in a separate column if you need it.

## CRUD

```csharp
SQLiteTable<Project> projects = db.Table<Project>();

await projects.AddAsync(new Project { ... });            // INSERT, sets Id back
await projects.AddRangeAsync(items);                     // wraps in a transaction
await projects.AddOrUpdateAsync(item);                   // INSERT OR REPLACE, won't override an existing id when it's [AutoIncrement] and it's not default.
await projects.UpdateAsync(item);                        // by primary key
await projects.RemoveAsync(item);                        // by primary key
await projects.RemoveRangeAsync(items);
await projects.ClearAsync();                             // DELETE all rows
await db.Schema.DropTableAsync<Project>();               // DROP TABLE
```

Bulk (no round-trip through .NET):

```csharp
await projects.Where(p => p.CategoryId == 5).ExecuteDeleteAsync();

await projects.Where(p => p.CategoryId == 5)
    .ExecuteUpdateAsync(s => s
        .Set(p => p.Name, "Renamed")
        .Set(p => p.Description, p => p.Description + " (archived)"));
```

Upsert with `ON CONFLICT`:

```csharp
projects.Upsert(p, c => c.OnConflict(x => x.Id).DoUpdateAll());
projects.Upsert(p, c => c.OnConflict(x => new { x.Id, x.CategoryId }).DoUpdate(x => x.Name));
projects.Upsert(p, c => c.OnConflict(x => x.Id).DoNothing());
```

Insert from a query:

```csharp
db.Table<ProjectArchive>().InsertFromQuery(
    db.Table<Project>()
        .Where(p => !p.IsActive)
        .Select(p => new ProjectArchive { Id = p.Id, Name = p.Name }));
```

## Querying

`db.Table<T>()` returns `SQLiteTable<T>`, which implements `IQueryable<T>`. Chain any LINQ method on it; a terminal method runs the query.

```csharp
List<Project> all     = await projects.ToListAsync();
Project[]      arr    = await projects.ToArrayAsync();
Project?       one    = await projects.FirstOrDefaultAsync(p => p.Id == id);
Project        single = await projects.SingleAsync(p => p.Id == id);
int            count  = await projects.CountAsync(p => p.CategoryId == 1);
bool           any    = await projects.AnyAsync(p => p.Name == "x");
decimal        sum    = await db.Table<ProjectTask>().SumAsync(t => t.SortOrder);
Dictionary<int,string> dict =
    await projects.ToDictionaryAsync(p => p.Id, p => p.Name);
```

Compose freely:

```csharp
var page = await projects
    .Where(p => p.CategoryId == catId)
    .OrderBy(p => p.Name)
    .Skip((pageIndex - 1) * pageSize)
    .Take(pageSize)
    .Select(p => new { p.Id, p.Name })
    .ToListAsync();
```

`Contains` for `IN`:

```csharp
int[] ids = [1, 2, 3];
await projects.Where(p => ids.Contains(p.Id)).ToListAsync();
```

## Joins

No navigation properties — write joins explicitly:

```csharp
var rows = await (
    from t in db.Table<ProjectTask>()
    join p in db.Table<Project>() on t.ProjectId equals p.Id
    where p.CategoryId == catId
    select new { t.Title, ProjectName = p.Name }
).ToListAsync();
```

Left join: `into` + `DefaultIfEmpty()`. Cross join: chain `from`s without `join`. Mix inner and left freely.

## Subqueries

Any `IQueryable` works inside `Where`. `Contains` produces `IN (SELECT …)`. Aggregates on subqueries (`Max`, `Min`, `Count`) are scalar. Inner queries can reference outer-row columns (correlated subqueries).

## Grouping

Query syntax for SQL `GROUP BY`:

```csharp
var stats = await (
    from p in db.Table<Project>()
    group p by p.CategoryId into g
    where g.Count() > 1            // becomes HAVING
    select new { CategoryId = g.Key, Count = g.Count() }
).ToListAsync();
```

`db.Table<T>().GroupBy(...).ToListAsync()` materialised to `IGrouping<,>` builds the groups in memory (no SQL `GROUP BY`). The source generator handles common key shapes (property access, anonymous types, simple arithmetic/predicates).

## Expressions translated to SQL

Inside `Where` / `Select`:

- Arithmetic: `+ - * / %`.
- Strings: `Length`, `ToUpper`, `ToLower`, `Trim`, `Contains`, `StartsWith`, `EndsWith`, `Replace`, `Substring`, `IndexOf`, `+` / `Concat`, `string.Join`, `string.IsNullOrEmpty`/`IsNullOrWhiteSpace`. `StringComparison.OrdinalIgnoreCase` works on `Contains` / `StartsWith` / `EndsWith`.
- Math: `Math.Abs`, `Round`, `Floor`, `Ceiling`, `Pow`, `Sqrt`, `Exp`, `Log`, `Log10`, `Sign`, `Max`, `Min`.
- `DateTime` / `DateOnly` / `TimeOnly` / `DateTimeOffset` / `TimeSpan`: components (`Year`, `Month`, `Day`, `Hour`, `DayOfWeek`, etc.) and arithmetic methods (`AddDays`, `Subtract`, etc.).
- `??` → `COALESCE`.
- Captured locals are bound as parameters automatically.

## Transactions

```csharp
await using SQLiteTransaction tx = await db.BeginTransactionAsync();
await db.Table<Project>().AddAsync(p);
await db.Table<ProjectTask>().AddRangeAsync(tasks, runInTransaction: false);
await tx.CommitAsync();
// no Commit -> auto rollback on dispose
```

Nested transactions use SQLite savepoints. Pass `separateConnection: true` for a dedicated-connection transaction (file DB only). Keep them short — they hold the write lock.

## Raw SQL

```csharp
// Wrapped in a subquery; selects every mapped column
var rows = await db.FromSql<Project>(
    "SELECT * FROM Project WHERE CategoryId = @cat",
    new SQLiteParameter { Name = "@cat", Value = 5 }
).ToListAsync();

// Direct execution, no wrapping. Column names must match property names (alias if not).
var dtos = db.Query<MyDto>(
    "SELECT Name AS Title, Id FROM Project WHERE CategoryId = @cat",
    new { cat = 5 });

int affected = await db.ExecuteAsync("DELETE FROM Project WHERE Id = @id", new { id = 5 });
int count = db.ExecuteScalar<int>("SELECT COUNT(*) FROM Project")!;
```

Available direct methods: `Query`, `QueryFirst`, `QueryFirstOrDefault`, `QuerySingle`, `QuerySingleOrDefault`, `ExecuteScalar`, `Execute` (plus `Async` versions).

Inspect generated SQL for any LINQ query:

```csharp
string sql = db.Table<Project>().Where(p => p.CategoryId == 5).ToSql();
SQLiteCommand cmd = db.Table<Project>().Where(p => p.CategoryId == 5).ToSqlCommand();
```

## Hooks (cross-cutting behaviour)

Configured on the options builder:

```csharp
.OnAdd<Project>(p => p.CreatedAt = DateTime.UtcNow)
.OnUpdate<Project>(p => p.UpdatedAt = DateTime.UtcNow)
.OnRemove<Project>((db, p) => { p.IsDeleted = true; db.Table<Project>().Update(p); return false; })
.AddQueryFilter<ISoftDelete>(e => !e.IsDeleted)
```

`AddQueryFilter<T>` (registration type may be an interface) is auto-applied to every query for matching entities and to `ExecuteUpdate` / `ExecuteDelete`. Opt out per query with `.IgnoreQueryFilters()`. `OnAction` is the cross-entity, AOT-safe shape; it returns the action to actually run (`Add`, `Update`, `Remove`, `AddOrUpdate`, `Skip`).

## Multi-threading

A single shared `SQLiteDatabase` is thread-safe — every command acquires an internal connection lock. With WAL mode on, reads never block writers and concurrent writers can run in parallel. Do not pass a `SQLiteTransaction` between threads; it belongs to the async flow that opened it.

## AOT and the source generator

For `PublishAot=true`:

1. Reference `SQLite.Framework.SourceGenerator` and call `.UseGeneratedMaterializers()` on the builder.
2. Add a `TrimmerRootDescriptor.xml` preserving every type listed under "Data types" above (the trimmer would otherwise strip them).
3. On methods that build `Select` projections directly, the compiler may emit `IL2026` warnings. Suppress with `[UnconditionalSuppressMessage("AOT", "IL2026", Justification = "...")]`.
4. Use `.DisableReflectionFallback()` in tests / CI to fail fast on shapes the generator does not cover.

The generator runs per-project; each project that builds queries needs its own reference and call to `.UseGeneratedMaterializers()`.

## Common pitfalls

- `AddAsync` always lets SQLite assign an `[AutoIncrement]` id; any value you set is overwritten. Use `AddOrUpdateAsync` to insert at a specific id.
- `[NotMapped]` collections are not loaded by queries — fill them explicitly.
- `decimal` loses precision past ~15 digits (stored as `double`). For exact arithmetic, store as string.
- `DateTimeOffset` round-trip drops the offset.
- Passing a `SQLiteTransaction` across threads is unsupported.
- Inside an outer transaction, pass `runInTransaction: false` to `AddRangeAsync` / `UpdateRangeAsync` / `RemoveRangeAsync` to avoid a redundant savepoint.
- `FromSql<T>` wraps the SQL and references every mapped column. If the source query is missing a column, it throws. Either select all columns, project into a narrower type, or use `Query<T>` (no wrapping).
