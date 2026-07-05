# Migrations

Migrations bring a live database up to the current model. There are no migration files to scaffold and no snapshot to keep in sync. The model you declare through attributes and `OnModelCreating` (see [Defining Models](Defining%20Models) and [Schema](Schema)) is the single target. Each version says which tables changed and the runner computes the DDL by comparing the model against the live database.

Reach the runner with `db.Schema.Migrations()`.

```csharp
await db.Schema.Migrations()
    .Version(1, m => m.CreateTable<Book>().CreateTable<Author>())
    .Version(2, m => m.TableChanged<Author>())
    .MigrateAsync();
```

## Declaring versions

`Version(n, build)` declares the work for one schema version. Version numbers must be one or more and each number may be declared only once. Versions are applied in ascending order no matter the order you declare them in.

The runner records the version it reached in `PRAGMA user_version`, so a version that already ran is skipped on the next run. On a fresh database `user_version` is 0 and every version runs in order, which makes the same migration chain serve new installs and upgrades alike.

## Never change a shipped version

A version that has reached any user is history. Treat it as read-only.

The runner skips every version at or below the recorded `user_version`, so an edit to an old version never runs on databases that already passed it. Only fresh installs see the edit. The result is two groups of users with different schemas and no error to tell you about it.

The same rule covers additions. There is no reason to add a new table to the first version. A fresh database runs the whole chain from the first version to the last, so a table created in version 12 exists on a new install just the same as one created in version 1.

```csharp
await db.Schema.Migrations()
    .Version(1, m => m.CreateTable<Book>())
    .Version(2, m => m.CreateTable<Review>())
    .MigrateAsync();
```

When you need a new table, a new column or new seed rows, declare the next version. The only time editing a version is fine is while you develop it, before it has shipped anywhere. Delete your local database file and let the chain run again.

## How a run works

- The runner reads `user_version` and keeps only the declared versions above it.
- The whole run happens in one transaction. When a step fails, the run rolls back to the version it started at and the next run retries from there.
- When the run succeeds, the runner writes the highest declared version to `user_version` and commits.

Within one run the order is fixed:

1. `RunBefore` callbacks.
2. Column renames.
3. Table creates.
4. One reconcile per table marked with `TableChanged`.
5. Column drops, table drops, row inserts, raw SQL steps and `Run` callbacks.

So a data step always runs against the final shape of the table and a `RunBefore` callback always sees the old shape. A table created in the same run skips its reconcile and drop-column steps, because the create already produced the current model. The `Set` values from `TableChanged` still apply. The runner writes them later in the run, just before the data steps of the version that set them. This way a new database ends up with the same data as an old one that went through each version.

Migrations always move toward the current model. There is no path back to an older version and no way to stop below the highest declared version. Do not set `user_version` by hand when you use migrations (see [Pragmas](Pragmas)).

## TableChanged

`TableChanged<T>()` reconciles the table for `T` to the current model. What it does:

* Creates the table when it does not exist.
* Adds new columns in place and drops columns the model no longer has. When a change cannot be made in place, it rebuilds the table the way SQLite recommends. It creates a new table from the model, copies the rows, drops the old table and renames the new one.
* Preserves the rows for every column the model keeps. A removed column loses its data, a new column gets NULL or its default and a type change keeps the values.
* Creates or recreates declared indexes and triggers and drops indexes that are no longer declared. Triggers that are not declared on the model are left alone.

FTS5 and R-Tree tables are only ensured to exist.

The in-place path uses `ALTER TABLE DROP COLUMN`, which needs SQLite 3.35 or newer, so on the default `SQLite.Framework` package it is marked as needing iOS 15. Pass `rebuild: true` to skip the in-place attempt and always rebuild, which works on any SQLite version the framework supports.

```csharp
.Version(2, m => m.TableChanged<Book>(rebuild: true))
```

## Filling new columns

A new `NOT NULL` column with no default cannot be filled by copying old rows. If the table has rows, the run stops with a clear error that names the column. You have three ways to fix it. Give the column a default in `OnModelCreating`, make it nullable or pass values to `TableChanged`.

`TableChanged<T>(s => s.Set(...))` fills or overrides columns during the reconcile. Each value is read from the old row. The runner unions the fills from every pending version before it reconciles, so a column added in a later version does not make an earlier version stop.

```csharp
await db.Schema.Migrations()
    .Version(1, m => m.TableChanged<Book>(s => s
        .Set(b => b.Status, "active")          // constant for every row
        .Set(b => b.Slug, b => b.Title)))      // expression over the old row
    .MigrateAsync();
```

The expression form is translated to SQL and runs over the old row, the same way CHECK and computed columns are. To read or write a column that has no CLR property, use `SQLiteColumn.Of<T>(row, "Name")`:

```csharp
await db.Schema.Migrations()
    .Version(1, m => m.TableChanged<Book>(s => s
        .Set(b => SQLiteColumn.Of<string>(b, "Slug"), b => b.Title)))
    .MigrateAsync();
```

A column you do not set is copied across unchanged when it still exists.

## Renames, drops and data steps

A reconcile cannot tell a rename from a drop plus an add, so rename a column with an explicit step. Renames are applied before the reconcile, so the data is kept.

```csharp
await db.Schema.Migrations()
    .Version(1, m => m
        .RenameColumn<Book>("BookTitle", "Title")
        .TableChanged<Book>())
    .MigrateAsync();
```

A step can also insert rows with `Insert`, drop a column with `DropColumn`, drop a table with `DropTable` or run raw SQL with `Sql` for a data fix. These run at the end of the run, in the fixed order above. `Insert` writes through the same pipeline as `Add`, so storage modes, converters and hooks apply, which makes it the right tool for seed rows. `InsertIfMissing` is the same insert but it skips every row whose key value is already in the table, which covers seeds that users may already have, see [Data Seeding](Data%20Seeding). `Sql` is raw SQL, so values are inlined and storage-mode conversions are on you. To move data out of a column you are removing, read it with `RunBefore` and write it back with `Run`, or keep the old column on the model while you copy it and remove it in a later version.

Renames and drops are tolerant. A rename whose source column is missing is skipped. A drop is skipped when the column is already gone or when the model still declares it.

## One file per migration

To keep each version in its own file instead of one long chain, implement `ISQLiteMigration` once per version, put the classes in a `Migrations` folder and register them with `Add<T>()`.

```csharp
// Migrations/M0001_InitialSchema.cs
public sealed class M0001_InitialSchema : ISQLiteMigration
{
    public static int Version => 1;

    public void Apply(SQLiteMigrationStep step)
    {
        step.CreateTable<Book>()
            .CreateTable<Author>();
    }
}
```

```csharp
// Migrations/M0002_AddBookGenre.cs
public sealed class M0002_AddBookGenre : ISQLiteMigration
{
    public static int Version => 2;

    public void Apply(SQLiteMigrationStep step)
    {
        step.TableChanged<Book>(s => s.Set(b => b.Genre, "Unknown"));
    }
}
```

```csharp
await db.Schema.Migrations()
    .Add<M0001_InitialSchema>()
    .Add<M0002_AddBookGenre>()
    .MigrateAsync();
```

`Add<T>()` reads the version number without creating an instance. The migration class is constructed only when its version is applied, so a class that has already run is never loaded into memory.

## Run callbacks

`Run` gives a version a callback for data work the typed steps cannot express. The callback gets a `SQLiteMigrationContext` with the database, the version the run started from, the version the run moves to and the cancellation token. It runs in the data phase, after every schema change, inside the migration transaction, so a throw rolls the whole run back.

`Run` takes a sync callback, so use the sync database methods inside it. `RunAsync` takes a callback that is awaited, so use the async methods there and pass the context token to them. Only `MigrateAsync` can await these callbacks. `Migrate` throws when a pending version declares one.

Declare callbacks from a migration class like the ones above, so each operation is a named method instead of a long lambda chain.

```csharp
// Migrations/M0003_NormalizeSlugs.cs
public sealed class M0003_NormalizeSlugs : ISQLiteMigration
{
    public static int Version => 3;

    public void Apply(SQLiteMigrationStep step)
    {
        step.RunAsync(NormalizeSlugs);
    }

    private static async Task NormalizeSlugs(SQLiteMigrationContext ctx)
    {
        List<Book> books = await ctx.Database.Table<Book>().ToListAsync(ctx.CancellationToken);
        foreach (Book book in books)
        {
            book.Slug = book.Title.Trim().ToLowerInvariant();
            await ctx.Database.Table<Book>().UpdateAsync(book, ctx.CancellationToken);
        }
    }
}
```

`RunBefore` and `RunBeforeAsync` are the same callbacks but they run before any schema change, against the old shape of the tables. Use them to read data that the schema changes would drop or rewrite, then write it back with `Run`. The class is constructed when its version applies, so a field can carry what `RunBefore` read over to the `Run` that writes it back.

Keep two things in mind inside `RunBefore`. The model describes the new shape, so a typed query can name columns that do not exist yet, which makes raw SQL through `Query` and `Execute` the safer tool. And on a fresh database the callback runs before any table exists, so guard reads with `TableExists`.

```csharp
// Migrations/M0004_MoveLegacySlugs.cs
public sealed class M0004_MoveLegacySlugs : ISQLiteMigration
{
    private readonly Dictionary<long, string> oldSlugs = [];

    public static int Version => 4;

    public void Apply(SQLiteMigrationStep step)
    {
        step.RunBefore(ReadOldSlugs)
            .TableChanged<Book>()
            .Run(WriteSlugs);
    }

    private void ReadOldSlugs(SQLiteMigrationContext ctx)
    {
        if (!ctx.Database.Schema.TableExists<Book>())
        {
            return;
        }

        foreach (Dictionary<string, object?> row in ctx.Database.Query<Dictionary<string, object?>>(
            "SELECT Id, LegacySlug FROM Books WHERE LegacySlug IS NOT NULL"))
        {
            oldSlugs[(long)row["Id"]!] = (string)row["LegacySlug"]!;
        }
    }

    private void WriteSlugs(SQLiteMigrationContext ctx)
    {
        foreach (Book book in ctx.Database.Table<Book>().ToList())
        {
            if (oldSlugs.TryGetValue(book.Id, out string? slug))
            {
                book.Slug = slug;
                ctx.Database.Table<Book>().Update(book);
            }
        }
    }
}
```

A plan lists a callback as `run callback at version 4`, so `Plan` stays readable without running anything. Work done inside a callback is not part of the count `Migrate` returns.

## See what a migration would do

`Plan()` reads the version recorded in the database and reports what a migrate would run, without changing anything.

```csharp
SQLiteMigrationPlan plan = await db.Schema.Migrations()
    .Version(1, m => m.TableChanged<Book>())
    .PlanAsync();

if (!plan.IsUpToDate)
{
    foreach (string step in plan.Operations)
    {
        Console.WriteLine(step);
    }
}
```

## Relation to the rest of the schema API

The runner reconciles structure and runs the data steps you declare. The [Schema](Schema) page covers the pieces around it:

- `CreateTable` is idempotent, so an app whose schema never changes shape can call it on startup and skip migrations entirely.
- `ValidateModel` compares the model against the live database and reports drift, useful in tests or at startup.
- `AddColumn`, `RenameColumn`, `DropColumn` and `RenameTable` on `db.Schema` make one-off changes outside a migration.
