# Query Filters

A query filter is a predicate you register once that the framework injects into every query against an entity. The call sites stay clean and nobody can forget the filter. The two classic uses are soft delete and multi-tenancy, both shown below.

## Registering a filter

Filters are registered on the options builder, not in `OnModelCreating`.

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .AddQueryFilter<Book>(b => !b.IsDeleted)
    .Build();

// The filter is applied automatically.
List<Book> books = await db.Table<Book>().ToListAsync();

// It composes with your own Where.
List<Book> cheap = await db.Table<Book>().Where(b => b.Price < 10).ToListAsync();
```

Multiple filters registered for the same type are AND-combined.

## Interface registrations

The registration type can be an interface or a base type. The filter then applies to every entity assignable to it, so one line covers the whole model.

```csharp
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}

builder.AddQueryFilter<ISoftDelete>(e => !e.IsDeleted);
```

The framework rewrites the filter's parameter to the concrete entity type when it injects it.

## Where filters apply

Filters are injected wherever a filtered table appears in a query:

* The root table of any query, including `Count`, `Any` and the other aggregates.
* Both sides of a `Join` and `GroupJoin`.
* Correlated subqueries and captured tables inside a query.
* The bodies of [CTEs](Common%20Table%20Expressions).
* `ExecuteUpdate` and `ExecuteDelete`, so a bulk write cannot touch filtered-out rows.
* `db.ReadOnlyTable<T>()` queries.

A filter body may itself query another table. When that table has its own filter, it is applied too. Mutually referencing filters are guarded against infinite recursion.

## What filters do not cover

* Entity-level writes by primary key. `Add`, `Update`, `Remove`, `AddOrUpdate` and `Upsert` target the row by key and do not inject filters, so code holding a reference to a soft-deleted entity can still update it.
* [Raw SQL](Raw%20SQL) through `FromSql`. You wrote the SQL, so you own the predicate.

## Opting out

`IgnoreQueryFilters()` drops every registered filter for one query. Your own `Where` still runs.

```csharp
List<Book> all = await db.Table<Book>().IgnoreQueryFilters().ToListAsync();
```

The opt-out is global to the query it appears in. One `IgnoreQueryFilters()` call anywhere in the chain, even inside a joined source or a correlated subquery, drops the filters for the whole statement, including the root table and every subquery. There is no per-table opt-out within a single query.

## Soft delete

Combine an interface filter with the `OnRemove` hook, which can turn a delete into an update by returning `false`.

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .AddQueryFilter<ISoftDelete>(e => !e.IsDeleted)
    .OnRemove<Book>((db, b) =>
    {
        b.IsDeleted = true;
        db.Table<Book>().Update(b);
        return false;
    })
    .Build();
```

Reads no longer see deleted rows, `Remove` marks instead of deleting and `IgnoreQueryFilters()` is the admin view that sees everything. Keep in mind that `ExecuteDelete` still deletes for real, it only skips rows the filter hides.

## Multi-tenancy

A filter can capture outside state. The captured member is read again every time a query is translated, not once at registration, so the filter follows the current value.

```csharp
public class TenantContext
{
    public int TenantId { get; set; }
}

TenantContext tenant = new();

SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .AddQueryFilter<ITenantOwned>(e => e.TenantId == tenant.TenantId)
    .Build();
```

Every query now only sees the current tenant's rows. Two notes:

* Writes are not filtered, so stamp the tenant on new rows yourself. The `OnAdd` hook is the natural place, `.OnAdd<Order>(o => o.TenantId = tenant.TenantId)`.
* The filter captures the `TenantContext` object, so keep one instance per database scope and update its value, rather than rebuilding options per tenant.
