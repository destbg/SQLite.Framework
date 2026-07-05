# Triggers

A trigger runs SQL inside the database when a row is inserted, updated or deleted. SQLite runs every trigger once per changed row, so the body can read the affected row through `OLD` and `NEW`. There are no statement-level triggers in SQLite.

The framework gives you three ways to work with triggers. A raw SQL body, a typed LINQ body that is checked at compile time and model triggers that are created and migrated together with the table.

## Raw SQL triggers

`db.Schema.CreateTrigger<T>(...)` creates a trigger on the table for `T`. The body and the optional `WHEN` predicate are raw SQL strings. Use `OLD` and `NEW` to refer to the row.

```csharp
await db.Schema.CreateTriggerAsync<Book>(
    name: "trg_book_history",
    timing: SQLiteTriggerTiming.After,
    @event: SQLiteTriggerEvent.Update,
    body: "INSERT INTO BookHistory(BookId, OldPrice, NewPrice) VALUES (NEW.BookId, OLD.BookPrice, NEW.BookPrice)",
    when: "OLD.BookPrice <> NEW.BookPrice");

await db.Schema.DropTriggerAsync("trg_book_history");
```

`SQLiteTriggerTiming` is `Before`, `After` or `InsteadOf`. `SQLiteTriggerEvent` is `Insert`, `Update` or `Delete`. `InsteadOf` only works on [Views](Views).

## Typed LINQ triggers

A second `CreateTrigger` overload builds the body from LINQ instead of a SQL string. Column names and the `WHEN` guard are checked at compile time and values are translated to SQL the same way queries are.

```csharp
await db.Schema.CreateTriggerAsync<Book>("trg_book_history", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Update, t => t
    .When(() => t.Old.Price != t.New.Price)
    .Insert(db.Table<BookHistory>(), s => s
        .Set(h => h.BookId, _ => t.New.Id)
        .Set(h => h.OldPrice, _ => t.Old.Price)
        .Set(h => h.NewPrice, _ => t.New.Price)));
```

The builder works like this:

* `t.Old` is the row before the change, valid for `UPDATE` and `DELETE` triggers. `t.New` is the row after the change, valid for `INSERT` and `UPDATE` triggers. They map to SQLite's `OLD` and `NEW` rows and are only valid inside the expressions passed to the builder.
* `When(() => ...)` sets the trigger's `WHEN` guard, so the body runs only for rows where the predicate is true. It can be called once per trigger.
* Each `Update`, `Insert` or `Delete` call adds one statement to the body, so one trigger can run several statements.
* `Update(target, predicate, setters)` updates rows in another table. The predicate and the setter values can read the target row and `t.Old` / `t.New`.
* `Insert(target, values)` inserts one row. Each `Set` pairs a target column with a value expression.
* `Delete(target, predicate)` deletes matching rows.

## Model triggers

`CreateTrigger` creates the trigger right away and is not tracked by the model. To make a trigger part of the model, declare it with `Trigger(...)` in `OnModelCreating`. Model triggers are created by `CreateTable` and a `TableChanged` [migration](Migrations) creates them when they are missing and recreates them when their body changes. Triggers that are not declared on the model are left alone.

Inside `OnModelCreating`, reach the target table through the database's own `Table<TTarget>()`, which is in scope.

```csharp
protected override void OnModelCreating(SQLiteModelBuilder builder)
{
    builder.Entity<Book>()
        .Trigger("trg_Book_Audit", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, t => t
            .Insert(Table<AuditLog>(), s => s.Set(a => a.BookId, _ => t.New.Id)));
}
```

## Use cases

### Audit log

Record every price change with the old and new value. The trigger writes the history row in the same transaction as the update, so the log cannot miss a change or record one that was rolled back.

```csharp
builder.Entity<Book>()
    .Trigger("trg_Book_PriceHistory", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Update, t => t
        .When(() => t.Old.Price != t.New.Price)
        .Insert(Table<PriceHistory>(), s => s
            .Set(h => h.BookId, _ => t.New.Id)
            .Set(h => h.OldPrice, _ => t.Old.Price)
            .Set(h => h.NewPrice, _ => t.New.Price)));
```

### Denormalized counters

Keep a count column on the parent in step with its child rows, so reads never need the aggregate.

```csharp
builder.Entity<OrderItem>()
    .Trigger("trg_OrderItem_CountUp", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, t => t
        .Update(Table<Order>(), o => o.Id == t.New.OrderId, s => s
            .Set(o => o.ItemCount, o => o.ItemCount + 1)))
    .Trigger("trg_OrderItem_CountDown", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Delete, t => t
        .Update(Table<Order>(), o => o.Id == t.Old.OrderId, s => s
            .Set(o => o.ItemCount, o => o.ItemCount - 1)));
```

The counter is updated by the database itself, so it stays correct no matter which code path writes the child table.

### Keeping FTS5 in sync

You do not need to write these by hand. An external-content FTS5 table declared with `AutoSync = FtsAutoSync.Triggers` gets the standard insert, update and delete sync triggers generated for you. See [Full Text Search](Full%20Text%20Search). To change the generated trigger shapes, override the trigger methods on a `SQLiteSchema` subclass, described under customizing schema generation on the [Schema](Schema) page.

## Behavior notes

* A trigger fires for every row the statement changes, including rows changed by [bulk operations](Bulk%20Operations) like `ExecuteUpdate` and `ExecuteDelete`.
* By default a trigger's own writes do not fire other triggers recursively. Turn that on with the `RecursiveTriggers` pragma, see [Pragmas](Pragmas).
* There is no update-of-columns filter in the API. Use a `When` guard comparing `t.Old` and `t.New` to react to specific column changes.
* `DropTrigger(name)` removes a trigger by name. Dropping a table drops its triggers with it.
