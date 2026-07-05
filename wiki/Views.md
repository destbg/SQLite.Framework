# Views

A view is a named `SELECT` stored in the database. The framework creates views from LINQ expressions and queries them through a read-only table, which makes a view the natural home for a read model, a projection that many call sites share.

## Creating a view

`db.Schema.CreateView<T>(...)` creates a view from a LINQ expression. The entity class describes the view's columns and its `[Table]` attribute names the view. The body is the SQL produced by translating the lambda.

```csharp
[Table("vBookSummary")]
public class BookSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
}

await db.Schema.CreateViewAsync<BookSummary>(() =>
    from b in db.Table<Book>()
    where b.Price > 0
    select new BookSummary { Id = b.Id, Title = b.Title, Price = b.Price });
```

The body can use anything the query translator supports, including joins, grouping and subqueries. The DDL uses `CREATE VIEW IF NOT EXISTS`, so calling `CreateView` twice is safe.

SQLite does not allow placeholders inside view bodies, so any constants in the lambda are inlined as SQL literals when the view is created. Only simple types (numbers, strings, bool) work as inlined constants. For exotic types use raw SQL through `db.Execute`. Query filters and captured values are baked into the view body with the values they had at create time. A view body cannot run code in memory, so a projection that needs it makes `CreateView` throw.

When the view entity renames a column with `[Column]` or `HasColumnName`, the view is created with an explicit column list, so reads through the entity find the renamed columns.

## Querying a view

Pair the view with `db.ReadOnlyTable<T>()`. It has the full LINQ surface (`Select`, `Where`, `Join` and the rest), the same as `db.Table<T>()`, but exposes no mutation methods.

```csharp
List<BookSummary> cheap = await db.ReadOnlyTable<BookSummary>()
    .Where(s => s.Price < 20)
    .OrderBy(s => s.Title)
    .ToListAsync();
```

`ReadOnlyTable<T>()` is not limited to views. It also fits SQLite system tables such as `sqlite_master` and any user table you want to expose as read-only.

## Read models

A view earns its place when the same projection is needed in several queries. Without one, each call site repeats the `Select` and the join that feeds it. With one, the shape lives in the database and every call site queries a simple table.

Views also compose. A query against `ReadOnlyTable<BookSummary>()` can filter, sort, join and aggregate over the view like any table. SQLite flattens the view body into the outer query when it plans.

For a projection used in only one or two places, a plain `Select` or a [CTE](Common%20Table%20Expressions) is usually enough. Reach for a view when the read model is part of your schema, not one query's convenience.

## Changing a view's definition

`CREATE VIEW IF NOT EXISTS` never replaces an existing view, so editing the lambda does nothing to a database that already has the old definition. Views are not tracked by the model either, so [migrations](Migrations) do not reconcile them. Drop and recreate to change one:

```csharp
await db.Schema.DropViewAsync<BookSummary>();
await db.Schema.CreateViewAsync<BookSummary>(() => ...);
```

Running that pair on startup is a simple way to keep views current. The cost is one cheap DDL statement per start.

## Writable views

A view is read-only in SQLite unless it has `INSTEAD OF` triggers that turn writes into statements against the underlying tables. Create one with `SQLiteTriggerTiming.InsteadOf` on the view's entity, see [Triggers](Triggers). `ReadOnlyTable<T>()` exposes no write methods, so route such writes through [Raw SQL](Raw%20SQL) or write to the underlying tables directly.

## Existence checks and cleanup

```csharp
bool exists = await db.Schema.ViewExistsAsync<BookSummary>();
IReadOnlyList<string> views = db.Schema.ListViews();

await db.Schema.DropViewAsync<BookSummary>();
await db.Schema.DropViewAsync("vBookSummary");
```
