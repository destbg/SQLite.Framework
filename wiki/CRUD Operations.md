# CRUD Operations

Get a table reference with `db.Table<T>()`. All operations return the number of rows affected.

```csharp
var books = db.Table<Book>();
```

## Create Table

```csharp
await db.Schema.CreateTableAsync<Book>();
```

Uses `CREATE TABLE IF NOT EXISTS`, so it is safe to call on every startup. If you have `[Indexed]` attributes on your model, the indexes are created at the same time. See [Schema](Schema) for the full set of DDL operations.

## Drop Table

```csharp
await db.Schema.DropTableAsync<Book>();
// Or drop by name if you don't have the model class
await db.Schema.DropTableAsync("Books");
```

Uses `DROP TABLE IF EXISTS`.

## Add

```csharp
var book = new Book { Title = "Clean Code", AuthorId = 1, Price = 29.99m };
await db.Table<Book>().AddAsync(book);
```

If the primary key has `[AutoIncrement]`, SQLite assigns the value and writes it back to the property on your object after the insert, so you can read the new id straight off `book.Id`. The same is true for `AddRangeAsync`, which sets the id on each entity in the list.

`AddAsync` and `AddRangeAsync` always let SQLite assign the id when the primary key is `[AutoIncrement]`, even if you have already set a value on the property. The value you set is ignored and gets overwritten with the generated id. If you want to insert a row at a specific id, use `AddOrUpdateAsync` and set the id on the entity before calling it.

## Add Many

```csharp
var newBooks = new List<Book>
{
    new() { Title = "Clean Code", AuthorId = 1, Price = 29.99m },
    new() { Title = "The Pragmatic Programmer", AuthorId = 2, Price = 35.00m },
    new() { Title = "Refactoring", AuthorId = 1, Price = 40.00m },
};

await db.Table<Book>().AddRangeAsync(newBooks);
```

`AddRangeAsync` wraps all inserts in a transaction by default for better performance. Pass `runInTransaction: false` if you want to add them one by one (which is much slower).

## Add or Update

```csharp
await db.Table<Book>().AddOrUpdateAsync(book);
```

Uses `INSERT OR REPLACE`. If a row with the same primary key already exists it is replaced, otherwise a new row is inserted. This is useful when you want to sync data without checking whether a record already exists.

When the primary key is `[AutoIncrement]`, the value you set on the object decides what happens. Leave it at its default (`0` for an `int` Id) and SQLite assigns a new id, which is then written back to the property. Set it to a non-default value and that id is used directly: an existing row with that id is replaced, or a new row is inserted at that id if none exists. The same applies to `AddOrUpdateRangeAsync`, which decides per entity in the list.

## Add or Update Many

```csharp
await db.Table<Book>().AddOrUpdateRangeAsync(newBooks);
```

Same as `AddOrUpdateAsync` but for a collection. Runs in a transaction by default.

## Update

```csharp
var book = await db.Table<Book>().FirstAsync(b => b.Id == 1);
book.Price = 24.99m;

await db.Table<Book>().UpdateAsync(book);
```

Update matches the row by primary key. Every other column is updated.

## Update Many

```csharp
var list = await db.Table<Book>().Where(b => b.AuthorId == 1).ToListAsync();

foreach (var book in list)
    book.Price *= 0.9m;

await db.Table<Book>().UpdateRangeAsync(list);
```

Like `AddRangeAsync`, this runs in a transaction by default.

## Execute Update with a predicate

```csharp
await db.Table<Book>()
    .Where(b => b.Genre == "Fiction")
    .ExecuteUpdateAsync(s => s
        .Set(b => b.InStock, false)
    );
```

Uses SQLite's `UPDATE ... SET ... WHERE ...` syntax to update rows matching the predicate without loading them into memory. The lambda specifies how to update each row, with access to the current values.

## Remove

```csharp
var book = await db.Table<Book>().FirstAsync(b => b.Id == 1);
await db.Table<Book>().RemoveAsync(book);
```

The model must have a `[Key]` property, Remove matches the row by that key. To remove rows in tables without a `[Key]` property, see [Bulk Operations](Bulk%20Operations).

## Remove Many

```csharp
var toDelete = await db.Table<Book>().Where(b => b.InStock == false).ToListAsync();
await db.Table<Book>().RemoveRangeAsync(toDelete);
```

Like `AddRangeAsync`, this runs in a transaction by default.

## Execute Remove with a predicate

```csharp
await db.Table<Book>().Where(b => b.InStock == false).ExecuteRemoveAsync();
```

Uses SQLite's `DELETE FROM ... WHERE ...` syntax to delete rows matching the predicate without loading them into memory.

## Clear All Rows

```csharp
await db.Table<Book>().ClearAsync();
```

Deletes every row in the table. The table itself does not get deleted, for that use `db.Schema.DropTableAsync<T>()`.

## Sync Versions

Every method above has a synchronous version without the `Async` suffix:

```csharp
db.Table<Book>().Add(book);
db.Table<Book>().AddRange(newBooks);
db.Table<Book>().AddOrUpdate(book);
db.Table<Book>().AddOrUpdateRange(newBooks);
db.Table<Book>().Update(book);
db.Table<Book>().UpdateRange(list);
db.Table<Book>().Remove(book);
db.Table<Book>().RemoveRange(toDelete);
db.Table<Book>().Clear();
db.Table<Book>().Schema.CreateTable();
db.Schema.DropTable<Book>();
```

## Insert with a conflict choice

`AddOrUpdate` defaults to `INSERT OR REPLACE`. Pass an `SQLiteConflict` value to pick one of SQLite's other conflict-resolution clauses:

```csharp
// Add the book, but if a row with the same primary key already exists, keep the old one.
db.Table<Book>().AddOrUpdate(book, SQLiteConflict.Ignore);

// Add the book, fail loudly on conflict.
db.Table<Book>().AddOrUpdate(book, SQLiteConflict.Abort);
```

The values map directly to SQLite's clauses: `Replace` (default), `Ignore`, `Abort`, `Fail`, `Rollback`. `AddOrUpdateRange` takes the same parameter.

## Upsert with `ON CONFLICT (...) DO UPDATE`

Use `Upsert` for SQLite's richer `ON CONFLICT (...) DO UPDATE` upsert syntax. Pick the conflict target column or columns and what to do on conflict:

```csharp
// Do nothing if a row with the same Id is already there.
db.Table<Book>().Upsert(book, c => c.OnConflict(b => b.Id).DoNothing());

// On conflict, copy every non-key column from the new row to the existing one.
db.Table<Book>().Upsert(book, c => c.OnConflict(b => b.Id).DoUpdateAll());

// On conflict, only update Title and Price.
db.Table<Book>().Upsert(book, c => c.OnConflict(b => b.Id).DoUpdate(b => b.Title, b => b.Price));

// Composite conflict target.
db.Table<Book>().Upsert(book, c => c.OnConflict(b => new { b.AuthorId, b.Title }).DoUpdate(b => b.Price));
```

`UpsertRange` is the range version. There is also `UpsertAsync` and `UpsertRangeAsync`.

## Hooks

Hooks let you mutate an entity right before a write or skip the default operation. They are registered on `SQLiteOptionsBuilder` and fire in registration order. Use them when subclassing `SQLiteTable<T>` would be too heavy.

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    // Audit timestamps
    .OnAdd<Book>(b => b.CreatedAt = DateTime.UtcNow)
    .OnUpdate<Book>(b => b.UpdatedAt = DateTime.UtcNow)
    // Soft delete: flip the flag, run an UPDATE, skip the DELETE.
    .OnRemove<Book>((db, book) =>
    {
        book.IsDeleted = true;
        db.Table<Book>().Update(book);
        return false;
    })
    .Build();
```

Two flavours per verb:

- `OnAdd<T>(Action<T> hook)`. Always continues with the default INSERT.
- `OnAdd<T>(Func<SQLiteDatabase, T, bool> hook)`. Return `false` to skip the default INSERT and any later hooks.

The same shape works for `OnUpdate`, `OnRemove`, and `OnAddOrUpdate`. The `OnAddOrUpdate` hooks fire for both `AddOrUpdate` and `Upsert`. Hooks for the Range methods fire per row, so if a hook returns `false` for one row that row is skipped and the rest still run.

Hooks run before any subclass override of the protected helpers, so the two compose: a hook on `OnAdd<Book>` mutates the entity, then a subclass override of `AddOrRemoveItem` sees the mutated entity.

## Cross-cutting action hooks

`OnAction` runs before every CRUD action across every entity. The hook gets the entity (untyped) and the action the framework was about to perform, and returns the action to actually run. The hook can also mutate the entity.

This is the AOT-safe way to react to a marker interface across all entities, without per-entity registration and without assembly scanning. The interface check happens inside the hook, not at registration.

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .OnAction((db, entity, action) =>
    {
        if (action == SQLiteAction.Remove && entity is ISoftDelete soft)
        {
            soft.IsDeleted = true;
            return SQLiteAction.Update;
        }
        return action;
    })
    .Build();
```

The hook returns one of:

- `SQLiteAction.Skip`. No SQL is issued for this row.
- `SQLiteAction.Add`, `SQLiteAction.Update`, `SQLiteAction.Remove`. Run the standard INSERT, UPDATE, or DELETE.
- `SQLiteAction.AddOrUpdate`. Run `INSERT OR REPLACE`. For `Upsert`, this keeps the configured `ON CONFLICT` clause.

Multiple `OnAction` hooks chain in registration order. Each hook receives the action returned by the previous one and can rewrite it again.

Per-entity hooks (`OnAdd<T>` and friends) run first. If they return `false` the action hooks do not fire and no SQL is issued.

## Customizing CRUD behaviour

For deeper changes that the hooks above cannot express (custom SQL, replacing how parameters are bound, replacing schema operations), you can subclass `SQLiteTable<T>` and override any of the protected helpers. The public `Add`, `AddRange`, `Update`, `UpdateRange`, `Remove`, `RemoveRange`, `AddOrUpdate`, `AddOrUpdateRange`, `Upsert`, and `UpsertRange` methods all funnel through these helpers, so a single override applies to every entry point.

| Override | Purpose |
|---|---|
| `GetAddInfo()` | Change the `INSERT` SQL or the column set used for inserts. |
| `GetUpdateInfo()` | Change the `UPDATE` SQL, add columns to the SET clause, or change the WHERE shape. |
| `GetRemoveInfo()` | Change the `DELETE` SQL. Useful for soft delete: return an `UPDATE` that flips a flag instead. |
| `GetAddOrUpdateInfo(SQLiteConflict)` | Change the `INSERT OR <action>` SQL used by `AddOrUpdate`. |
| `GetUpsertInfo(configure)` | Change the `INSERT INTO ... ON CONFLICT (...) DO ...` SQL used by `Upsert`. |
| `WrapParam(placeholder, column)` | Wrap parameters with custom SQL functions, for example `jsonb(@p0)`. |
| `AddOrRemoveItem(columns, sql, item)` | Mutate the entity right before binding, for example to stamp `CreatedAt`. Called by Add, Remove, AddOrUpdate, and Upsert. |
| `UpdateItem(columns, primaryColumns, sql, item)` | Same as above but for Update. |
| `Clear()` | Replace the row-clear operation entirely. To customize DDL, subclass `SQLiteSchema` and register it with `UseSchema`. |

Example: an auditing table that stamps a row counter on every insert.

```csharp
public class AuditingTable : SQLiteTable<Book>
{
    public AuditingTable(SQLiteDatabase database, TableMapping table) : base(database, table) { }

    public int InsertCount { get; private set; }

    protected override int AddOrRemoveItem(TableColumn[] columns, string sql, Book item)
    {
        InsertCount++;
        return base.AddOrRemoveItem(columns, sql, item);
    }
}
```

To use a subclass, expose it from a custom `SQLiteDatabase`:

```csharp
public class MyDatabase : SQLiteDatabase
{
    public MyDatabase(SQLiteOptions options) : base(options) { }

    public AuditingTable Books => field ??= new AuditingTable(this, TableMapping(typeof(Book)));
}
```

Then use it as you would any other table:

```csharp
db.Table<Book>().Schema.CreateTable();
db.Books.Add(new Book { ... });
```

`SQLite.Framework.SourceGenerator` walks up the inheritance chain when it scans `SQLiteTable<T>`-typed properties, so a subclass-typed property like `AuditingTable Books` registers `Book` as an entity for materializer generation, just like a property typed as `SQLiteTable<Book>` would.
