# Window Functions

The `SQLite.Framework.Window` add-in package lets you use SQLite window functions inside LINQ queries. Window functions compute a value for each row based on a set of rows related to it, called a window. Unlike aggregate functions in a GROUP BY, window functions do not collapse rows so you still get one output row per input row.

Install it alongside whichever core package you use:

```
dotnet add package SQLite.Framework
dotnet add package SQLite.Framework.Window
```

> **Platform compatibility.** Basic window functions were added in SQLite 3.25.0, but full support including the GROUPS frame type and expression-based PRECEDING/FOLLOWING boundaries requires [SQLite 3.28.0](https://www.sqlite.org/windowfunctions.html) (2019-04-16). Android API level 30 (Android 11) ships with SQLite 3.28.0. iOS 13 ships with SQLite 3.28.0 as well. Older Android and iOS versions do not include window function support.
>
> If you need to support Android API 29 or earlier, or iOS 12 or earlier, use `SQLite.Framework.Bundled` or `SQLite.Framework.Cipher` instead of the default `SQLite.Framework` package. Both ship their own SQLite binary and work on any supported OS version.

## Setup

Call `AddWindow()` on the `SQLiteOptionsBuilder` before calling `Build()`:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .AddWindow()
    .Build();

using var db = new SQLiteDatabase(options);
```

This registers translators for every method on `SQLiteWindowFunctions` and `FrameBoundary`. After that you can use them inside any LINQ `Select`.

## Building a window

Every window expression starts with a function call followed by `.Over()`. The `.Over()` call produces an empty window that covers the entire result set. You then chain further methods to narrow it down.

```csharp
db.Table<Order>().Select(o => new
{
    o.Id,
    RowNum = SQLiteWindowFunctions.RowNumber()
        .Over()
        .OrderBy(o.Id)
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

By default SQLite uses a range from the start of the partition to the current row when an ORDER BY is present. You can set an explicit frame with `Rows`, `Range`, or `Groups`, using the `FrameBoundary` helpers to specify each end.

```csharp
SQLiteWindowFunctions.Sum(o.Amount)
    .Over()
    .OrderBy(o.Date)
    .Rows(FrameBoundary.UnboundedPreceding(), FrameBoundary.CurrentRow())
```

| Boundary | SQL produced |
|---|---|
| `FrameBoundary.UnboundedPreceding()` | `UNBOUNDED PRECEDING` |
| `FrameBoundary.CurrentRow()` | `CURRENT ROW` |
| `FrameBoundary.UnboundedFollowing()` | `UNBOUNDED FOLLOWING` |
| `FrameBoundary.Preceding(n)` | `n PRECEDING` |
| `FrameBoundary.Following(n)` | `n FOLLOWING` |

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
    })
    .ToListAsync();
```

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
    })
    .ToListAsync();
```

## Native AOT

`AddWindow()` carries `[DynamicDependency]` attributes that tell the trimmer to keep all public methods on `SQLiteWindowFunctions` and `FrameBoundary`, so those marker methods are never removed from the output. No extra setup is needed.
