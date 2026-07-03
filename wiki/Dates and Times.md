# Dates and Times

SQLite has no date type, so every library picks a representation. This page explains the framework's choices, how to query date parts and the pitfalls around `Kind`, offsets and data written by other tools. The full mode and format tables live on [Storage Options](Storage%20Options), the divergences from .NET are pinned on [Limitations](Limitations).

## How values are stored

By default `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly` and `TimeSpan` are all stored as their .NET tick count in an INTEGER column. Ticks compare, order and subtract as plain integers, which is what makes `Where`, `OrderBy` and date-part access translatable to SQL.

Each type also has TEXT modes, set globally on the options builder:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .UseDateTimeStorage(DateTimeStorageMode.TextFormatted, "yyyy-MM-dd HH:mm:ss")
    .Build();
```

The trade is simple. Tick storage supports querying date parts (`Year`, `Month`, `DayOfWeek` and so on) but is not human readable and not directly usable by other tools. Formatted text is readable and interoperable but a value stored as text compares as a string and date-part access in `Where` or `OrderBy` throws. `TextTicks` exists only for compatibility with older sqlite-net-pcl databases, see [Migrating from sqlite-net-pcl](Migrating%20from%20sqlite-net-pcl).

## DateTime.Kind

Tick storage keeps only the tick count, so `Kind` is not stored. Every value reads back with `Kind == DateTimeKind.Unspecified`, whether you wrote it as UTC or local.

The robust convention is to store UTC everywhere and convert at the edges of the app. If you must round-trip `Kind`, use `TextFormatted` with the round-trip format `"o"`. The framework parses with `DateTimeStyles.RoundtripKind`, so a format that encodes the zone brings `Kind` back.

## DateTimeOffset

The default `Ticks` mode stores the local clock ticks and drops the offset, so values read back with a zero offset. Worse, two rows written with different offsets compare by their local ticks, not by the instant, so comparisons, ordering and subtraction across offsets can disagree with .NET, which normalizes to UTC first.

Pick per use case:

* `UtcTicks` stores the UTC instant. Comparisons and ordering match .NET. Date parts read in a query (`.Year`, `.Hour`) come back in UTC. The offset is still not stored, keep a second column when you need it back.
* `TextFormatted` keeps the offset in the string, but no date-part queries.

```csharp
builder.UseDateTimeOffsetStorage(DateTimeOffsetStorageMode.UtcTicks);
```

If none of that matters because all writers use one offset, the default is fine.

## Now in queries

`DateTime.UtcNow` in a query expression is evaluated by your app when the query runs and bound as an ordinary parameter. That is almost always what you want and it is testable, see [Testing](Testing).

```csharp
DateTime cutoff = DateTime.UtcNow.AddDays(-30);
var recent = await db.Table<Order>().Where(o => o.CreatedAt >= cutoff).ToListAsync();
```

To get the database engine's clock instead, use the marker functions, `SQLiteDateFunctions.Datetime()` for the current date and time or `SQLiteFunctions.UnixEpoch()` for unix seconds. They translate to SQLite's own functions and evaluate inside the engine at query time.

## Querying date parts

Under tick storage the usual properties translate to SQL, `Year`, `Month`, `Day`, `Hour`, `Minute`, `Second`, `Millisecond`, `Microsecond`, `Nanosecond`, `DayOfWeek`, `DayOfYear`, `Date` and `TimeOfDay`, plus the `TimeSpan` components and totals.

```csharp
var mondays = await db.Table<Order>()
    .Where(o => o.CreatedAt.DayOfWeek == DayOfWeek.Monday)
    .ToListAsync();

var byYear = await db.Table<Order>()
    .GroupBy(o => o.CreatedAt.Year)
    .Select(g => new { Year = g.Key, Count = g.Count() })
    .ToListAsync();
```

Arithmetic translates too. The `Add*` family, `Subtract` and `DateTime` minus `DateTime` yielding a `TimeSpan`. `AddMonths` and `AddYears` clamp to the end of the month the way .NET does. Two known divergences are pinned in [Limitations](Limitations). Fractional `Add*` amounts can land one tick away from .NET and month math that lands in December 9999 overflows SQLite's date range.

## Interop with data from other tools

Tick integers are the framework's format, not SQLite's. SQLite's own date functions expect ISO text, julian day numbers or unix seconds. This matters in two directions.

Reading columns another tool wrote:

* ISO text like `2023-06-15 12:00:00` reads back correctly even under the default mode, the reader falls back to a general parse for TEXT values. To also write that shape, switch to `TextFormatted`.
* Unix seconds in an INTEGER column are misread as ticks, which produces dates in year 0001. There is no unix-seconds mode. Either map the column as `long` and convert in code or read it through `SQLiteDateFunctions.Datetime(o.CreatedAtUnix, "unixepoch")`.

Using raw SQL against tick columns, a plain `strftime('%Y', Created)` gives the wrong answer on ticks. Convert first:

```sql
strftime('%Y', (Created - 621355968000000000) / 10000000, 'unixepoch')
```

`621355968000000000` is the tick count of 1970-01-01, so the expression turns ticks into unix seconds. The framework's LINQ translation does this same conversion for you internally.

## Pitfalls at a glance

* Tick storage reads back `Kind == Unspecified`. Store UTC by convention.
* Default `DateTimeOffset` storage compares by local ticks. Use `UtcTicks` to compare by instant.
* Date parts in `Where` and `OrderBy` need tick storage. Under text modes they throw.
* Values stored as text order as strings. `TextTicks` in particular orders wrong across digit-count boundaries.
* A `TimeSpan` column stored as `Text` cannot be added to a `DateTime` in SQL.

The [Limitations](Limitations) page has the complete list with exact behavior.
