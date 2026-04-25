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

## Changes counters

```csharp
long sinceLastWrite = db.Table<Book>().Select(b => SQLiteFunctions.Changes()).First();
long sinceConnectionOpen = db.Table<Book>().Select(b => SQLiteFunctions.TotalChanges()).First();
```

## Calling outside a query

These methods throw `InvalidOperationException` if you call them outside a LINQ query. They are markers for the translator, not real C# code.
