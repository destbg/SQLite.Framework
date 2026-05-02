# Overview

A short tour of `SQLite.Framework` on one page. Each section links to a deeper guide if you want more detail.

## What it is

A small ORM that lets you use LINQ on a SQLite database. If you have used Entity Framework Core before, most things will feel familiar. The main difference is that this library does not have a change tracker, navigation properties, or migrations. It is built to be fast and to work with Native AOT.

`db.Table<T>()` returns a `SQLiteTable<T>`. That class implements `IQueryable<T>`, so any LINQ method works on it. Every method has an async version. Drop the `Async` suffix when you want the sync version.

## Packages

| Package | When to use |
|---|---|
| `SQLite.Framework` | Default. Uses the SQLite that ships with the operating system. |
| `SQLite.Framework.Bundled` | Ships its own SQLite binary. Use it when the OS version is too old. |
| `SQLite.Framework.Cipher` | SQLCipher for encrypted databases. |
| `SQLite.Framework.Base` | No SQLite provider included. You bring your own. |
| `SQLite.Framework.DependencyInjection` | `AddSQLiteDatabase` for `IServiceCollection`. |
| `SQLite.Framework.SourceGenerator` | Build-time materializers. Required for AOT. |

The first four packages expose the same API and assembly name, so you can swap between them without changing your code. Make sure all installed packages have the same version.

See [Getting Started](Getting%20Started) for installation and the first run.

## Setup

Plain console:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .UseWalMode()                     // optional, allows concurrent writes
    .UseGeneratedMaterializers()      // requires SQLite.Framework.SourceGenerator
    .DisableReflectionFallback()      // throws if a query needs runtime reflection
    .Build();

using SQLiteDatabase db = new(options);
await db.Schema.CreateTableAsync<Project>();
```

With dependency injection:

```csharp
services.AddSQLiteDatabase<AppDatabase>(b =>
{
    b.DatabasePath = dbPath;
    b.UseWalMode()
        .UseGeneratedMaterializers()
        .DisableReflectionFallback();
});
```

The default lifetime is `Singleton`, which is the right choice for desktop and mobile apps. See [Dependency Injection](Dependency%20Injection) for more.

A custom subclass keeps your tables in one place and gives you an async hook for schema setup. Use the async versions so the UI thread does not block on disk I/O at startup:

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

A model is a plain class. The attributes come from `System.ComponentModel.DataAnnotations`, `System.ComponentModel.DataAnnotations.Schema`, and `SQLite.Framework.Attributes`.

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

Keep entity classes to the columns of the table. There are no navigation properties. To load related rows, query them yourself or build a DTO with a `Select` projection or a join.

The most common attributes:

- `[Key]` plus `[AutoIncrement]`. SQLite assigns the id and writes it back to the entity after `AddAsync`.
- `[Required]`. The column is `NOT NULL`. Nullable types like `string?` or `int?` map to nullable columns.
- `[Indexed]`. Creates an index. You can make it unique, give it a name, or make it composite by using the same name on more than one column.
- `[Column("...")]` and `[Table("...")]`. Rename a column or a table.
- `[WithoutRowId]`. A class-level attribute. The primary key must not be `[AutoIncrement]`.
- `[NotMapped]`. Leave a property out of the database. Useful for the rare case where you need a derived value on the class itself.

Schema setup is safe to call on every startup because it uses `CREATE TABLE IF NOT EXISTS`. Track migrations through `db.Pragmas.UserVersion`. See [Defining Models](Defining%20Models) for the full list.

## Data types

| .NET | SQLite | Notes |
|---|---|---|
| `int`, `long`, `short`, `byte`, `bool`, `enum` | INTEGER | |
| `DateTime`, `DateOnly`, `TimeOnly`, `TimeSpan`, `DateTimeOffset` | INTEGER | Stored as ticks. `DateTimeOffset` does not preserve the offset, so save it in a separate column if you need it. |
| `float`, `double` | REAL | |
| `decimal` | REAL | Stored as `double`, so values past about 15 to 16 digits can lose precision. |
| `string`, `char` | TEXT | |
| `Guid` | TEXT | Lowercase hyphenated. |
| `byte[]` | BLOB | |

All types also work as nullable. See [Data Types](Data%20Types) for the full list.

## CRUD

```csharp
SQLiteTable<Project> projects = db.Table<Project>();

await projects.AddAsync(new Project { ... });            // INSERT, sets Id back
await projects.AddRangeAsync(items);                     // wrapped in a transaction
await projects.AddOrUpdateAsync(item);                   // INSERT OR REPLACE
await projects.UpdateAsync(item);                        // by primary key
await projects.RemoveAsync(item);                        // by primary key
await projects.RemoveRangeAsync(items);
await projects.ClearAsync();                             // delete all rows
await db.Schema.DropTableAsync<Project>();               // DROP TABLE
```

Bulk operations skip the round-trip through .NET:

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

Insert from another query:

```csharp
db.Table<ProjectArchive>().InsertFromQuery(
    db.Table<Project>()
        .Where(p => !p.IsActive)
        .Select(p => new ProjectArchive { Id = p.Id, Name = p.Name }));
```

See [CRUD Operations](CRUD%20Operations) and [Bulk Operations](Bulk%20Operations).

## Querying

`db.Table<T>()` returns `SQLiteTable<T>`, which is an `IQueryable<T>`. You chain LINQ methods on it. The terminal method runs the query.

```csharp
List<Project> all    = await projects.ToListAsync();
Project[]     arr    = await projects.ToArrayAsync();
Project?      one    = await projects.FirstOrDefaultAsync(p => p.Id == id);
Project       single = await projects.SingleAsync(p => p.Id == id);
int           count  = await projects.CountAsync(p => p.CategoryId == 1);
bool          any    = await projects.AnyAsync(p => p.Name == "x");
decimal       sum    = await db.Table<ProjectTask>().SumAsync(t => t.SortOrder);
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

Use `Contains` for `IN`:

```csharp
int[] ids = [1, 2, 3];
await projects.Where(p => ids.Contains(p.Id)).ToListAsync();
```

See [Querying](Querying) for everything else.

## Joins

There are no navigation properties, joins are written explicitly:

```csharp
var rows = await (
    from t in db.Table<ProjectTask>()
    join p in db.Table<Project>() on t.ProjectId equals p.Id
    where p.CategoryId == catId
    select new { t.Title, ProjectName = p.Name }
).ToListAsync();
```

For a left join, add `into` and `DefaultIfEmpty()`. For a cross join, chain `from`s without `join`. You can mix inner and left joins freely. See [Joins](Joins).

## Subqueries

Any `IQueryable` can be used inside a `Where` clause as a subquery. `Contains` produces `IN (SELECT ...)`. Aggregates on a subquery (such as `Max`, `Min`, or `Count`) become a scalar value. An inner query can read columns from the outer row, which is a correlated subquery. See [Subqueries](Subqueries).

## Grouping

LINQ query syntax produces SQL `GROUP BY`:

```csharp
var stats = await (
    from p in db.Table<Project>()
    group p by p.CategoryId into g
    where g.Count() > 1            // becomes HAVING
    select new { CategoryId = g.Key, Count = g.Count() }
).ToListAsync();
```

If you call `db.Table<T>().GroupBy(...).ToListAsync()`, the rows come back without a SQL `GROUP BY` and the framework builds the groups in memory. The source generator handles common key shapes such as a single property, an anonymous type, or simple arithmetic. See [Grouping and Aggregates](Grouping%20and%20Aggregates).

## Expressions translated to SQL

Inside `Where` and `Select` you can use:

- Arithmetic: `+`, `-`, `*`, `/`, `%`.
- Strings: `Length`, `ToUpper`, `ToLower`, `Trim`, `Contains`, `StartsWith`, `EndsWith`, `Replace`, `Substring`, `IndexOf`, `+` and `Concat`, `string.Join`, `string.IsNullOrEmpty`, and `string.IsNullOrWhiteSpace`. `StringComparison.OrdinalIgnoreCase` works on `Contains`, `StartsWith`, and `EndsWith`.
- Math: `Math.Abs`, `Round`, `Floor`, `Ceiling`, `Pow`, `Sqrt`, `Exp`, `Log`, `Log10`, `Sign`, `Max`, `Min`.
- `DateTime`, `DateOnly`, `TimeOnly`, `DateTimeOffset`, and `TimeSpan` parts (`Year`, `Month`, `Day`, `Hour`, `DayOfWeek`, and so on) plus arithmetic methods (`AddDays`, `Subtract`, and friends).
- The `??` operator turns into `COALESCE`.
- Captured local variables become parameters automatically.

See [Expressions](Expressions) for the full list.

## Transactions

```csharp
await using SQLiteTransaction tx = await db.BeginTransactionAsync();
await db.Table<Project>().AddAsync(p);
await db.Table<ProjectTask>().AddRangeAsync(tasks, runInTransaction: false);
await tx.CommitAsync();
// no Commit -> auto rollback on dispose
```

Nested transactions use SQLite savepoints. Pass `separateConnection: true` to run a transaction on a dedicated connection (file databases only). Keep transactions short because they hold the write lock. See [Transactions](Transactions).

## Raw SQL

```csharp
// Wrapped in a subquery, so it selects every mapped column.
var rows = await db.FromSql<Project>(
    "SELECT * FROM Project WHERE CategoryId = @cat",
    new SQLiteParameter { Name = "@cat", Value = 5 }
).ToListAsync();

// Direct execution, no wrapping. Column names must match property names. Alias them in the SQL if they do not.
var dtos = db.Query<MyDto>(
    "SELECT Name AS Title, Id FROM Project WHERE CategoryId = @cat",
    new { cat = 5 });

int affected = await db.ExecuteAsync("DELETE FROM Project WHERE Id = @id", new { id = 5 });
int count = db.ExecuteScalar<int>("SELECT COUNT(*) FROM Project")!;
```

The direct methods are `Query`, `QueryFirst`, `QueryFirstOrDefault`, `QuerySingle`, `QuerySingleOrDefault`, `ExecuteScalar`, and `Execute`. Each one has an async version.

You can also see the SQL that LINQ would produce:

```csharp
string sql = db.Table<Project>().Where(p => p.CategoryId == 5).ToSql();
SQLiteCommand cmd = db.Table<Project>().Where(p => p.CategoryId == 5).ToSqlCommand();
```

See [Raw SQL](Raw%20SQL).

## Hooks and global filters

Configure them on the options builder:

```csharp
.OnAdd<Project>(p => p.CreatedAt = DateTime.UtcNow)
.OnUpdate<Project>(p => p.UpdatedAt = DateTime.UtcNow)
.OnRemove<Project>((db, p) =>
{
    p.IsDeleted = true;
    db.Table<Project>().Update(p);
    return false;
})
.AddQueryFilter<ISoftDelete>(e => !e.IsDeleted)
```

`AddQueryFilter<T>` runs on every query for matching entities, plus on `ExecuteUpdate` and `ExecuteDelete`. The registration type can be an interface, so you cover many entity types in one line. To skip filters in a single query, call `.IgnoreQueryFilters()`.

`OnAction` is the cross-entity hook that works well with AOT. Your hook returns the action to actually run, like `Add`, `Update`, `Remove`, `AddOrUpdate`, or `Skip`.

## Multi-threading

A single shared `SQLiteDatabase` is safe to use across many threads. Every command takes an internal lock. With WAL mode on, reads never wait for writers and many writers can run at the same time. Do not pass a `SQLiteTransaction` between threads. It belongs to the async flow that opened it. See [Multi-threading](Multi-threading).

## AOT and the source generator

When you publish with `PublishAot=true`:

1. Reference `SQLite.Framework.SourceGenerator` and call `.UseGeneratedMaterializers()` on the builder.
2. Add a `TrimmerRootDescriptor.xml` that preserves every type listed in the data types table above. The trimmer would otherwise remove them.
3. Methods that build `Select` projections directly may produce `IL2026` warnings at publish time. Suppress them with `[UnconditionalSuppressMessage("AOT", "IL2026", Justification = "...")]`.
4. Use `.DisableReflectionFallback()` in tests and CI to fail fast on shapes the generator does not cover.

The generator runs once per project. Each project that builds queries needs its own reference and its own call to `.UseGeneratedMaterializers()`. See [Native AOT](Native%20AOT) and [Source Generator](Source%20Generator).

## Common pitfalls

- `AddAsync` always lets SQLite assign the `[AutoIncrement]` id. Any value you set on the entity is overwritten. Use `AddOrUpdateAsync` to insert at a specific id.
- `[NotMapped]` collections are not loaded by queries. Fill them yourself.
- `decimal` loses precision past about 15 digits because it is stored as `double`. For exact arithmetic, store the value as a string.
- `DateTimeOffset` round trips drop the offset.
- A `SQLiteTransaction` is bound to the async flow that opened it. Do not pass it across threads.
- Inside an outer transaction, pass `runInTransaction: false` to `AddRangeAsync`, `UpdateRangeAsync`, and `RemoveRangeAsync` to avoid a redundant savepoint.
- `FromSql<T>` wraps your SQL in a subquery and selects every mapped column. If your SQL is missing a column, it throws. Either select all columns, project into a smaller type, or use `Query<T>`, which does not wrap.

## Working with an AI agent

If you write code with an AI coding agent, see [AI Assistance](AI%20Assistance) for a ready-made cheat sheet you can drop into your project.
