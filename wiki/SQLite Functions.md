# SQLite Functions

`SQLiteFunctions` is a static class with helpers for SQLite functions that have no plain C# equivalent. They work inside a LINQ query, and the framework swaps them for the right SQL.

The class also holds the FTS5 helpers (`Match`, `Rank`, `Snippet`, `Highlight`). Those have their own page in [Full Text Search](Full%20Text%20Search).

## What is in here

| Method | What it maps to |
|---|---|
| `Random()` | `RANDOM()` |
| `RandomBlob(n)` | `RANDOMBLOB(n)` |
| `Glob(pattern, value)` | `value GLOB pattern` |
| `UnixEpoch()` | `unixepoch()` |
| `UnixEpoch(when)` | `unixepoch(when)` |
| `Printf(format, args)` | `printf(format, ...)` |
| `Regexp(value, pattern)` | `value REGEXP pattern` |
| `Between(value, low, high)` | `value BETWEEN low AND high` |
| `In(value, v0, v1, ...)` | `value IN (v0, v1, ...)` |
| `Coalesce(v0, v1, ...)` | `coalesce(v0, v1, ...)` |
| `Nullif(a, b)` | `nullif(a, b)` |
| `Iif(condition, whenTrue, whenFalse)` | `iif(condition, whenTrue, whenFalse)` |
| `Typeof(x)` | `typeof(x)` |
| `Hex(bytes)` | `hex(bytes)` |
| `Unhex(value)` / `Unhex(value, ignoreChars)` | `unhex(...)` (SQLite 3.41+) |
| `Format(format, args)` | `format(format, ...)` (SQLite 3.38+) |
| `Unicode(value)` | `unicode(value)` |
| `Char(c0, c1, ...)` | `char(c0, c1, ...)` |
| `Quote(x)` | `quote(x)` |
| `Zeroblob(n)` | `zeroblob(n)` |
| `Instr(haystack, needle)` | `instr(haystack, needle)` |
| `LastInsertRowId()` | `last_insert_rowid()` |
| `SqliteVersion()` | `sqlite_version()` |
| `Min(v0, v1, ...)` | `min(v0, v1, ...)` (scalar, two or more args) |
| `Max(v0, v1, ...)` | `max(v0, v1, ...)` (scalar, two or more args) |
| `Total(g.Select(x => x.Col))` | `total(col)` (aggregate, returns `REAL`, `0.0` for empty input) |
| `Changes()` | `changes()` |
| `TotalChanges()` | `total_changes()` |

## Random and random blob

```csharp
List<Book> randomFive = await db.Table<Book>()
    .OrderBy(b => SQLiteFunctions.Random())
    .Take(5)
    .ToListAsync();

byte[] sessionToken = await db.Table<Book>()
    .Select(b => SQLiteFunctions.RandomBlob(16))
    .FirstAsync();
```

## GLOB matching

`Glob` is like `LIKE` but uses Unix shell wildcards. `*` matches any string, `?` matches one character.

```csharp
List<Book> rows = await db.Table<Book>()
    .Where(b => SQLiteFunctions.Glob("Clean*", b.Title))
    .ToListAsync();
```

The order is `Glob(pattern, value)`. The SQL is `value GLOB pattern`.

## Unix timestamps

```csharp
long now = await db.Table<Book>().Select(b => SQLiteFunctions.UnixEpoch()).FirstAsync();

long y2024 = await db.Table<Book>()
    .Select(b => SQLiteFunctions.UnixEpoch("2024-01-01"))
    .FirstAsync();
```

## Printf formatting

```csharp
string formatted = await db.Table<Book>()
    .Select(b => SQLiteFunctions.Printf("Book %d: %s", b.Id, b.Title))
    .FirstAsync();
```

## Regular expressions

`Regexp` only works when the SQLite build has a regex extension loaded. The default builds do not include one.

```csharp
List<Book> rows = await db.Table<Book>()
    .Where(b => SQLiteFunctions.Regexp(b.Title, "^[A-Z]"))
    .ToListAsync();
```

## Between

`Between(value, low, high)` is the same as `value >= low && value <= high`, but emits SQLite's `BETWEEN` operator. Both ends are inclusive. To get `NOT BETWEEN`, wrap the call with `!`.

```csharp
List<Book> rows = await db.Table<Book>()
    .Where(b => SQLiteFunctions.Between(b.Id, 2, 4))
    .ToListAsync();

List<Book> outside = await db.Table<Book>()
    .Where(b => !SQLiteFunctions.Between(b.Id, 2, 4))
    .ToListAsync();
```

## In

`In(value, v0, v1, ...)` checks whether `value` matches any of the listed values. The list can be a `params` argument list or a captured array.

```csharp
List<Book> picked = await db.Table<Book>()
    .Where(b => SQLiteFunctions.In(b.Id, 1, 3, 5))
    .ToListAsync();

int[] wanted = [1, 3, 5];
List<Book> sameThing = await db.Table<Book>()
    .Where(b => SQLiteFunctions.In(b.Id, wanted))
    .ToListAsync();

List<Book> excluded = await db.Table<Book>()
    .Where(b => !SQLiteFunctions.In(b.Id, 1, 3, 5))
    .ToListAsync();
```

## Coalesce and nullif

`Coalesce` picks the first non-null value. `Nullif(a, b)` returns null when `a == b`, otherwise `a`.

```csharp
string title = await db.Table<Book>()
    .Select(b => SQLiteFunctions.Coalesce(b.Title, "(untitled)"))
    .FirstAsync();

string? trimmed = await db.Table<Book>()
    .Select(b => SQLiteFunctions.Nullif(b.Title, ""))
    .FirstAsync();
```

## Type and encoding helpers

`Typeof` returns the SQLite storage class as a lowercase string (`"null"`, `"integer"`, `"real"`, `"text"`, `"blob"`). `Hex` returns the upper-case hex of a blob. `Quote` returns the SQL literal form of a value. `Zeroblob(n)` returns a blob of `n` zero bytes.

```csharp
string kind = await db.Table<Book>().Select(b => SQLiteFunctions.Typeof(b.Price)).FirstAsync();

byte[] data = [0xDE, 0xAD];
string hex = await db.Table<Book>().Select(b => SQLiteFunctions.Hex(data)).FirstAsync();

string literal = await db.Table<Book>().Select(b => SQLiteFunctions.Quote(b.Title)).FirstAsync();
byte[] padding = await db.Table<Book>().Select(b => SQLiteFunctions.Zeroblob(16)).FirstAsync();
```

## Instr

`Instr(haystack, needle)` returns the 1-based index of `needle` inside `haystack`, or `0` if not found.

```csharp
List<Book> withLph = await db.Table<Book>()
    .Where(b => SQLiteFunctions.Instr(b.Title, "lph") > 0)
    .ToListAsync();
```

## Per-row min and max

`Min` and `Max` here are the scalar form: they return the smallest or largest of their arguments **for each row**.

```csharp
List<int> floors = await db.Table<Book>()
    .Select(b => SQLiteFunctions.Min(b.Id, b.AuthorId))
    .ToListAsync();
```

> **Always pass two or more values.** Calling `SQLiteFunctions.Min(x)` or `SQLiteFunctions.Max(x)` with a single value compiles fine but is wrong. SQLite reads `min(x)` and `max(x)` as the aggregate forms, so the surrounding query silently turns into an aggregate query and returns one row instead of one per input row. For aggregates over a column, use LINQ's own `Queryable.Min` / `Queryable.Max` instead.

## Total aggregate

`Total` translates to SQLite's `total(X)` aggregate. It is like `Queryable.Sum` but always returns a `REAL` value and returns `0.0` for an empty input set instead of `NULL`. Pass a `Select` projection over a grouping enumerable.

```csharp
var revenue = await (
    from b in db.Table<Book>()
    group b by b.AuthorId into g
    select new
    {
        AuthorId = g.Key,
        Revenue = SQLiteFunctions.Total(g.Select(x => x.Price))
    }
).ToListAsync();
```

The SQL is:

```sql
SELECT b0."BookAuthorId" AS "AuthorId",
       total(b0."BookPrice") AS "Revenue"
FROM "Books" AS b0
GROUP BY b0."BookAuthorId"
```

`total` shines when the aggregated column has `NULL` values or when the projected input is empty. `sum` returns `NULL` in those cases. `total` returns `0.0` so callers do not need a special case for empty groups.

## Last insert rowid and SQLite version

```csharp
long newId = await db.Table<Book>().Select(b => SQLiteFunctions.LastInsertRowId()).FirstAsync();
string version = await db.Table<Book>().Select(b => SQLiteFunctions.SqliteVersion()).FirstAsync();
```

## Changes counters

```csharp
long sinceLastWrite = await db.Table<Book>().Select(b => SQLiteFunctions.Changes()).FirstAsync();
long sinceConnectionOpen = await db.Table<Book>().Select(b => SQLiteFunctions.TotalChanges()).FirstAsync();
```

## Date and time functions

`SQLiteDateFunctions` exposes SQLite's date and time SQL functions directly. Each accepts a time value plus any number of modifier strings like `"+7 days"`, `"start of month"`, `"unixepoch"`, or `"utc"`. Time values can be ISO 8601 strings, the literal `"now"`, Julian day numbers, or column references.

| Method | What it maps to |
|---|---|
| `Date()` | `date()` |
| `Date(when, modifiers)` | `date(when, modifiers)` |
| `Time()` | `time()` |
| `Time(when, modifiers)` | `time(when, modifiers)` |
| `Datetime()` | `datetime()` |
| `Datetime(when, modifiers)` | `datetime(when, modifiers)` |
| `JulianDay()` | `julianday()` |
| `JulianDay(when, modifiers)` | `julianday(when, modifiers)` |
| `Strftime(format, when, modifiers)` | `strftime(format, when, modifiers)` |
| `Timediff(when1, when2)` | `timediff(when1, when2)` (SQLite 3.43+, not available in SQLCipher) |

```csharp
string thisMonth = await db.Table<Book>()
    .Select(b => SQLiteDateFunctions.Strftime("%Y-%m", b.CreatedAt))
    .FirstAsync();

string nextWeek = await db.Table<Book>()
    .Select(b => SQLiteDateFunctions.Date(b.CreatedAt, "+7 days"))
    .FirstAsync();
```

## Calling outside a query

These methods throw `InvalidOperationException` if you call them outside a LINQ query. They are markers for the translator, not real C# code.
