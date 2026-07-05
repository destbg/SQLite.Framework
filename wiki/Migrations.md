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
2. Table renames.
3. Column renames.
4. Table creates.
5. One reconcile per table marked with `TableChanged`.
6. Column drops, table drops, row inserts, updates, deletes, view steps, full text search rebuilds, raw SQL steps and `Run` callbacks, in the order declared.

So a data step always runs against the final shape of the table and a `RunBefore` callback always sees the old shape. A table created in the same run skips its reconcile and drop-column steps, because the create already produced the current model. A dropped table that a later version creates again is dropped right before the create, so the new table starts empty with the current schema. The `Set` values from `TableChanged` run as updates in the data phase, just before the data steps of the version that set them, so each version's fill sees the rows the earlier versions inserted. A fill that reads a column outside the current model runs inside the reconcile instead, so it can still read the old column, and is skipped on a database that never had that column. This way a new database ends up with the same data as an old one that went through each version.

Migrations always move toward the current model. There is no path back to an older version and no way to stop below the highest declared version. Do not set `user_version` by hand when you use migrations (see [Pragmas](Pragmas)).

## When the database is newer than the app

A database can also be ahead of the code, for example after the app is downgraded or when an old build opens a file a newer build created. The recorded version is then above the highest declared one and the schema may not match the model. `Migrate` throws in this state instead of treating the database as up to date. `Plan` does not throw. It reports the state through `DatabaseIsNewer`, so you can show the user a clear message before you migrate.

```csharp
SQLiteMigrationPlan plan = await runner.PlanAsync();
if (plan.DatabaseIsNewer)
{
    // tell the user to update the app instead of calling MigrateAsync
}
```

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

A reconcile cannot tell a rename from a drop plus an add, so rename a table or a column with an explicit step. Renames are applied before every other schema change, so the data is kept.

`RenameColumn` renames a column on the table for the entity. Both names are SQLite column names.

```csharp
await db.Schema.Migrations()
    .Version(1, m => m
        .RenameColumn<Book>("BookTitle", "Title")
        .TableChanged<Book>())
    .MigrateAsync();
```

`RenameTable` renames a table to the name the model declares now. Pass the old SQLite table name. On a fresh database the old table does not exist, the chain creates the table under its current name from the start and the rename is skipped.

```csharp
await db.Schema.Migrations()
    .Version(1, m => m.CreateTable<Book>())
    .Version(2, m => m
        .RenameTable<Book>("Publications")
        .TableChanged<Book>())
    .MigrateAsync();
```

A step can also insert rows with `Insert`, drop a column with `DropColumn`, drop a table with `DropTable` or run raw SQL with `Sql` for a change the typed methods do not cover. These run at the end of the run, in the fixed order above. `Insert` writes through the same pipeline as `Add`, so storage modes, converters and hooks apply, which makes it the right tool for seed rows. `InsertIfMissing` is the same insert but it skips every row whose key value is already in the table, which covers seeds that users may already have, see [Data Seeding](Data%20Seeding). To move data out of a column you are removing, read it with `RunBefore` and write it back with `Run`, or keep the old column on the model while you copy it and remove it in a later version.

`Sql` takes optional parameters, so values go through normal binding instead of being pasted into the SQL text.

```csharp
await db.Schema.Migrations()
    .Version(4, m => m.Sql(
        "UPDATE \"Books\" SET \"Status\" = @status WHERE \"Status\" IS NULL",
        new SQLiteParameter { Name = "@status", Value = "active" }))
    .MigrateAsync();
```

Renames and drops are tolerant. A rename whose source table or column is missing is skipped. A drop is skipped when the column is already gone or when the model still declares it.

## Update and delete steps

`Update` and `Delete` are typed data fixes, the same as `ExecuteUpdate` and `ExecuteDelete` on a query. Use them to backfill or clean rows as part of a version, without raw SQL. Both run in the data phase, against the final shape of the table. Query filters are ignored, so every row is visible to the step (see [Query Filters](Query%20Filters)).

```csharp
await db.Schema.Migrations()
    .Version(5, m => m
        .TableChanged<Book>()
        .Update<Book>(b => b.Status == "unknown", s => s.Set(b => b.Status, "active"))
        .Delete<Book>(b => b.Title == ""))
    .MigrateAsync();
```

Without a predicate, `Update` updates every row and `Delete` deletes every row.

## Views

`CreateView` creates the view named after the entity, with the body produced by translating the query, the same as `CreateView` on the schema API. An existing view with that name is dropped first, so declaring the view again in a later version updates its body. `DropView` drops a view and is skipped when the view does not exist. Both run in the data phase, after every table change of the run.

```csharp
await db.Schema.Migrations()
    .Version(6, m => m.CreateView<BookSummary>(() =>
        from b in db.Table<Book>()
        where b.Status == "active"
        select new BookSummary { Id = b.Id, Title = b.Title }))
    .MigrateAsync();
```

## Rebuilding a full text search table

A reconcile only ensures that an FTS5 table exists, so a change to the FTS5 declaration needs an explicit step. `RebuildFullTextSearch` drops the FTS5 table with its sync triggers, recreates both from the model and refills the index from the content table with the FTS5 rebuild command. It requires a content table set with `ContentTable` on `[FullTextSearch]`, because the rows are read back from that table (see [Full Text Search](Full%20Text%20Search)).

```csharp
await db.Schema.Migrations()
    .Version(7, m => m.RebuildFullTextSearch<BookSearch>())
    .MigrateAsync();
```

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

## Watching progress

A long migration can rebuild large tables, which takes time on a phone. `Progress` registers a callback that fires once per operation, right before the operation is applied. Use it to drive an "updating your data" screen or to log what the runner does. Each update carries the version, the same description a plan shows, the one-based position in the run and the total operation count.

```csharp
await db.Schema.Migrations()
    .Progress(p => Console.WriteLine($"{p.Index}/{p.Count} v{p.Version}: {p.Description}"))
    .Version(1, m => m.TableChanged<Book>())
    .MigrateAsync();
```

The callback runs inside the migration transaction, so a throw rolls the whole run back.

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

## See the exact SQL

`Script()` goes one step further than `Plan`. It runs every pending version inside a transaction, collects each SQL statement the run executes, then rolls the transaction back. The version and the schema are left as they were. Use it to review the exact statements before an upgrade or to attach them to a bug report.

```csharp
IReadOnlyList<string> statements = await db.Schema.Migrations()
    .Version(1, m => m.TableChanged<Book>())
    .ScriptAsync();

foreach (string statement in statements)
{
    Console.WriteLine(statement);
}
```

Three things to keep in mind. The statements come from a real run, so on a large database a script takes as long as the migration itself and rows passed to `Insert` can get their auto-increment keys set. Callbacks declared with `Run` and `RunBefore` are not invoked and appear as SQL comments in their place. Statement parameters are inlined, so every entry runs on its own.

## Relation to the rest of the schema API

The runner reconciles structure and runs the data steps you declare. The [Schema](Schema) page covers the pieces around it:

- `CreateTable` is idempotent, so an app whose schema never changes shape can call it on startup and skip migrations entirely.
- `ValidateModel` compares the model against the live database and reports drift, useful in tests or at startup.
- `AddColumn`, `RenameColumn`, `DropColumn` and `RenameTable` on `db.Schema` make one-off changes outside a migration.
