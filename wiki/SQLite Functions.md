# SQLite Functions

`SQLiteFunctions` is a static class with helpers for SQLite functions that have no plain C# equivalent. Use these inside a LINQ query. The framework swaps them for the right SQL.

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
| `Typeof(x)` | `typeof(x)` |
| `Hex(bytes)` | `hex(bytes)` |
| `Quote(x)` | `quote(x)` |
| `Zeroblob(n)` | `zeroblob(n)` |
| `Instr(haystack, needle)` | `instr(haystack, needle)` |
| `LastInsertRowId()` | `last_insert_rowid()` |
| `SqliteVersion()` | `sqlite_version()` |
| `Min(v0, v1, ...)` | `min(v0, v1, ...)` (scalar, two or more args) |
| `Max(v0, v1, ...)` | `max(v0, v1, ...)` (scalar, two or more args) |
| `Changes()` | `changes()` |
| `TotalChanges()` | `total_changes()` |

## Random and random blob

```csharp
List<Book> randomFive = db.Table<Book>()
    .OrderBy(b => SQLiteFunctions.Random())
    .Take(5)
    .ToList();

byte[] sessionToken = db.Table<Book>()
    .Select(b => SQLiteFunctions.RandomBlob(16))
    .First();
```

## GLOB matching

`Glob` is like `LIKE` but uses Unix shell wildcards. `*` matches any string, `?` matches one character.

```csharp
List<Book> rows = db.Table<Book>()
    .Where(b => SQLiteFunctions.Glob("Clean*", b.Title))
    .ToList();
```

The order is `Glob(pattern, value)`. The SQL is `value GLOB pattern`.

## Unix timestamps

```csharp
long now = db.Table<Book>().Select(b => SQLiteFunctions.UnixEpoch()).First();

long y2024 = db.Table<Book>()
    .Select(b => SQLiteFunctions.UnixEpoch("2024-01-01"))
    .First();
```

## Printf formatting

```csharp
string formatted = db.Table<Book>()
    .Select(b => SQLiteFunctions.Printf("Book %d: %s", b.Id, b.Title))
    .First();
```

## Regular expressions

`Regexp` only works when the SQLite build has a regex extension loaded. The default builds do not include one.

```csharp
List<Book> rows = db.Table<Book>()
    .Where(b => SQLiteFunctions.Regexp(b.Title, "^[A-Z]"))
    .ToList();
```

## Between

`Between(value, low, high)` is the same as `value >= low && value <= high`, but emits SQLite's `BETWEEN` operator. Both ends are inclusive. To get `NOT BETWEEN`, wrap the call with `!`.

```csharp
List<Book> rows = db.Table<Book>()
    .Where(b => SQLiteFunctions.Between(b.Id, 2, 4))
    .ToList();

List<Book> outside = db.Table<Book>()
    .Where(b => !SQLiteFunctions.Between(b.Id, 2, 4))
    .ToList();
```

## In

`In(value, v0, v1, ...)` checks whether `value` matches any of the listed values. The list can be a `params` argument list or a captured array.

```csharp
List<Book> picked = db.Table<Book>()
    .Where(b => SQLiteFunctions.In(b.Id, 1, 3, 5))
    .ToList();

int[] wanted = [1, 3, 5];
List<Book> sameThing = db.Table<Book>()
    .Where(b => SQLiteFunctions.In(b.Id, wanted))
    .ToList();

List<Book> excluded = db.Table<Book>()
    .Where(b => !SQLiteFunctions.In(b.Id, 1, 3, 5))
    .ToList();
```

## Coalesce and nullif

`Coalesce` picks the first non-null value. `Nullif(a, b)` returns null when `a == b`, otherwise `a`.

```csharp
string title = db.Table<Book>()
    .Select(b => SQLiteFunctions.Coalesce(b.Title, "(untitled)"))
    .First();

string? trimmed = db.Table<Book>()
    .Select(b => SQLiteFunctions.Nullif(b.Title, ""))
    .First();
```

## Type and encoding helpers

`Typeof` returns the SQLite storage class as a lowercase string (`"null"`, `"integer"`, `"real"`, `"text"`, `"blob"`). `Hex` returns the upper-case hex of a blob. `Quote` returns the SQL literal form of a value. `Zeroblob(n)` returns a blob of `n` zero bytes.

```csharp
string kind = db.Table<Book>().Select(b => SQLiteFunctions.Typeof(b.Price)).First();

byte[] data = [0xDE, 0xAD];
string hex = db.Table<Book>().Select(b => SQLiteFunctions.Hex(data)).First();

string literal = db.Table<Book>().Select(b => SQLiteFunctions.Quote(b.Title)).First();
byte[] padding = db.Table<Book>().Select(b => SQLiteFunctions.Zeroblob(16)).First();
```

## Instr

`Instr(haystack, needle)` returns the 1-based index of `needle` inside `haystack`, or `0` if not found.

```csharp
List<Book> withLph = db.Table<Book>()
    .Where(b => SQLiteFunctions.Instr(b.Title, "lph") > 0)
    .ToList();
```

## Per-row min and max

`Min` and `Max` here are the scalar form: they return the smallest or largest of their arguments **for each row**.

```csharp
List<int> floors = db.Table<Book>()
    .Select(b => SQLiteFunctions.Min(b.Id, b.AuthorId))
    .ToList();
```

> **Always pass two or more values.** Calling `SQLiteFunctions.Min(x)` or `SQLiteFunctions.Max(x)` with a single value compiles fine but is wrong. SQLite reads `min(x)` and `max(x)` as the aggregate forms, so the surrounding query silently turns into an aggregate query and returns one row instead of one per input row. For aggregates over a column, use LINQ's own `Queryable.Min` / `Queryable.Max` instead.

## Last insert rowid and SQLite version

```csharp
long newId = db.Table<Book>().Select(b => SQLiteFunctions.LastInsertRowId()).First();
string version = db.Table<Book>().Select(b => SQLiteFunctions.SqliteVersion()).First();
```

## Changes counters

```csharp
long sinceLastWrite = db.Table<Book>().Select(b => SQLiteFunctions.Changes()).First();
long sinceConnectionOpen = db.Table<Book>().Select(b => SQLiteFunctions.TotalChanges()).First();
```

## Calling outside a query

These methods throw `InvalidOperationException` if you call them outside a LINQ query. They are markers for the translator, not real C# code.
