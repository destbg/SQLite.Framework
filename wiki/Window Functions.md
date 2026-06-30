# Window Functions

Window functions compute a value for each row based on a set of rows related to it, called a window. Unlike aggregate functions in a GROUP BY, window functions do not collapse rows so you still get one output row per input row. Support is built into every SQLite-provider package.

> **Platform compatibility.** Full window function support needs SQLite 3.28 or newer. Only iOS is affected, it needs iOS 13 or newer. Android always satisfies it because the package bundles its own SQLite there. To support older iOS use `SQLite.Framework.Bundled` which bundles its own SQLite on every platform.

## Building a window

Every window expression starts with a function call followed by `.Over()`. The `.Over()` call produces an empty window that covers the entire result set. You then chain further methods to narrow it down.

When you project into a typed DTO, the value unwraps to `T` automatically through an implicit conversion:

```csharp
db.Table<Order>().Select(o => new OrderWithRowNum
{
    Id = o.Id,
    RowNum = SQLiteWindowFunctions.RowNumber()
        .Over()
        .OrderBy(o.Id),
})
```

When you project into an anonymous type or a `var`, call `.AsValue()` at the end of the chain so the field type is `T` and not `SQLiteWindow<T>`:

```csharp
db.Table<Order>().Select(o => new
{
    o.Id,
    RowNum = SQLiteWindowFunctions.RowNumber()
        .Over()
        .OrderBy(o.Id)
        .AsValue(),
})
```

The methods are translated to SQL at query time. They throw `InvalidOperationException` if called outside a LINQ expression.

### Partition

`PartitionBy` splits the rows into independent groups. The window function resets at the start of each partition.

```csharp
SQLiteWindowFunctions.RowNumber()
    .Over()
    .PartitionBy(o.CustomerId)
    .OrderBy(o.Date)
```

Use `ThenPartitionBy` to add more partition columns:

```csharp
SQLiteWindowFunctions.RowNumber()
    .Over()
    .PartitionBy(o.Year)
    .ThenPartitionBy(o.CustomerId)
    .OrderBy(o.Date)
```

### Order

`OrderBy` and `OrderByDescending` control the order of rows within the window. Use `ThenOrderBy` and `ThenOrderByDescending` for secondary sort keys.

```csharp
SQLiteWindowFunctions.Rank()
    .Over()
    .PartitionBy(o.CustomerId)
    .OrderByDescending(o.Amount)
    .ThenOrderBy(o.Id)
```

### Frame

By default SQLite uses a range from the start of the partition to the current row when an ORDER BY is present. You can set an explicit frame with `Rows`, `Range`, or `Groups`, using the `SQLiteFrameBoundary` helpers to specify each end.

```csharp
SQLiteWindowFunctions.Sum(o.Amount)
    .Over()
    .OrderBy(o.Date)
    .Rows(SQLiteFrameBoundary.UnboundedPreceding(), SQLiteFrameBoundary.CurrentRow())
```

| Boundary | SQL produced |
|---|---|
| `SQLiteFrameBoundary.UnboundedPreceding()` | `UNBOUNDED PRECEDING` |
| `SQLiteFrameBoundary.CurrentRow()` | `CURRENT ROW` |
| `SQLiteFrameBoundary.UnboundedFollowing()` | `UNBOUNDED FOLLOWING` |
| `SQLiteFrameBoundary.Preceding(n)` | `n PRECEDING` |
| `SQLiteFrameBoundary.Following(n)` | `n FOLLOWING` |

#### Excluding rows from the frame

Pass a `SQLiteFrameExclude` as the last argument to `Rows`, `Range`, or `Groups` to drop rows near the current row from the frame the function sees. The default `NoOthers` keeps every row, so existing two-argument calls are unchanged. Any other value needs SQLite 3.28.0.

```csharp
SQLiteWindowFunctions.Sum(o.Amount)
    .Over()
    .OrderBy(o.Id)
    .Rows(
        SQLiteFrameBoundary.UnboundedPreceding(),
        SQLiteFrameBoundary.UnboundedFollowing(),
        SQLiteFrameExclude.CurrentRow)
```

| `SQLiteFrameExclude` | SQL produced |
|---|---|
| `NoOthers` | (no clause, the default) |
| `CurrentRow` | `EXCLUDE CURRENT ROW` |
| `Group` | `EXCLUDE GROUP` |
| `Ties` | `EXCLUDE TIES` |

`CurrentRow` leaves out the current row, `Group` also leaves out its peers (rows with the same ORDER BY value), and `Ties` leaves out the peers but keeps the current row.

## Aggregate functions

These compute a value over the rows in the window.

| Method | SQL produced |
|---|---|
| `Sum<T>(value)` | `SUM(value)` |
| `Avg<T>(value)` | `AVG(value)` |
| `Min<T>(value)` | `MIN(value)` |
| `Max<T>(value)` | `MAX(value)` |
| `Count()` | `COUNT(*)` |
| `Count<T>(value)` | `COUNT(value)` |

### Running total

```csharp
var results = await db.Table<Order>()
    .Select(o => new
    {
        o.Id,
        o.Amount,
        RunningTotal = SQLiteWindowFunctions.Sum(o.Amount)
            .Over()
            .PartitionBy(o.CustomerId)
            .OrderBy(o.Date)
            .AsValue(),
    })
    .ToListAsync();
```

### Filtered aggregate

Chain `Filter` to feed only the rows matching a predicate into the aggregate. It maps to `SUM(x) FILTER (WHERE pred) OVER (...)` and needs SQLite 3.30.0. This lets you compute several totals with different filters in a single pass. The chain order does not matter, because `FILTER` is always emitted before `OVER`.

```csharp
var results = await db.Table<Order>()
    .Select(o => new
    {
        o.CustomerId,
        BigOrderTotal = SQLiteWindowFunctions.Sum(o.Amount)
            .Filter(o.Amount > 100)
            .Over()
            .PartitionBy(o.CustomerId)
            .OrderBy(o.Date)
            .AsValue(),
    })
    .ToListAsync();
```

`Filter` only applies to aggregate window functions (`Sum`, `Avg`, `Min`, `Max`, `Count`). SQLite rejects it on ranking functions such as `RowNumber`.

## Ranking functions

These assign a numeric rank or position to each row within the window.

| Method | SQL produced |
|---|---|
| `RowNumber()` | `ROW_NUMBER()` |
| `Rank()` | `RANK()` |
| `DenseRank()` | `DENSE_RANK()` |
| `PercentRank()` | `PERCENT_RANK()` |
| `CumeDist()` | `CUME_DIST()` |
| `NTile(buckets)` | `NTILE(buckets)` |

`Rank` and `DenseRank` both rank rows by the ORDER BY columns, but `Rank` leaves gaps after a tie while `DenseRank` does not.

### Rank within a group

```csharp
var results = await db.Table<Order>()
    .Select(o => new
    {
        o.Id,
        o.CustomerId,
        Rank = SQLiteWindowFunctions.Rank()
            .Over()
            .PartitionBy(o.CustomerId)
            .OrderByDescending(o.Amount)
            .AsValue(),
    })
    .ToListAsync();
```

## Navigation functions

These look up values from other rows within the window relative to the current row.

| Method | SQL produced |
|---|---|
| `Lag<T>(value)` | `LAG(value)` |
| `Lag<T>(value, offset)` | `LAG(value, offset)` |
| `Lag<T>(value, offset, default)` | `LAG(value, offset, default)` |
| `Lead<T>(value)` | `LEAD(value)` |
| `Lead<T>(value, offset)` | `LEAD(value, offset)` |
| `Lead<T>(value, offset, default)` | `LEAD(value, offset, default)` |
| `FirstValue<T>(value)` | `FIRST_VALUE(value)` |
| `LastValue<T>(value)` | `LAST_VALUE(value)` |
| `NthValue<T>(value, n)` | `NTH_VALUE(value, n)` |

`Lag` looks back at a previous row and `Lead` looks forward. Both return `NULL` for rows where no such row exists unless you provide a default value.

### Compare to previous row

```csharp
var results = await db.Table<Order>()
    .Select(o => new
    {
        o.Id,
        o.Amount,
        PreviousAmount = SQLiteWindowFunctions.Lag(o.Amount, 1L, 0.0)
            .Over()
            .OrderBy(o.Date)
            .AsValue(),
    })
    .ToListAsync();
```

## Native AOT

The framework keeps all public methods on `SQLiteWindowFunctions` and `SQLiteFrameBoundary` rooted for the trimmer, so those marker methods are never removed from the output. No extra setup is needed.
