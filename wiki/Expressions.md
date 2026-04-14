# Expressions

LINQ expressions inside `Where`, `Select`, and other methods are translated directly to SQL. This page covers what is supported beyond basic comparisons.

## Arithmetic

The standard arithmetic operators all work:

```csharp
var results = await db.Table<Book>()
    .Where(b => (b.Price * 1.1) + 5 > 20)
    .ToListAsync();

var discounted = await db.Table<Book>()
    .Select(b => new { b.Title, Sale = b.Price * 0.9 })
    .ToListAsync();

var evens = await db.Table<Book>()
    .Where(b => b.Id % 2 == 0)
    .ToListAsync();
```

Supported operators: `+`, `-`, `*`, `/`, `%`.

## Math Functions

| C# | SQL |
|---|---|
| `Math.Abs(x)` | `ABS(x)` |
| `Math.Round(x)` | `ROUND(x)` |
| `Math.Round(x, digits)` | `ROUND(x, digits)` |
| `Math.Floor(x)` | `FLOOR(x)` |
| `Math.Ceiling(x)` | `CEIL(x)` |
| `Math.Pow(x, y)` | `POWER(CAST(x AS REAL), y)` |
| `Math.Sqrt(x)` | `SQRT(x)` |
| `Math.Exp(x)` | `EXP(x)` |
| `Math.Log(x)` | `LOG(x)` |
| `Math.Log(x, base)` | `LOG(x) / LOG(base)` |
| `Math.Log10(x)` | `LOG10(x)` |
| `Math.Sign(x)` | `CASE WHEN x > 0 THEN 1 WHEN x < 0 THEN -1 ELSE 0 END` |
| `Math.Max(x, y)` | `CASE WHEN x > y THEN x ELSE y END` |
| `Math.Min(x, y)` | `CASE WHEN x < y THEN x ELSE y END` |

```csharp
var results = await db.Table<Book>()
    .Where(b => Math.Abs(b.Price - 10) < 1)
    .ToListAsync();

var rounded = await db.Table<Book>()
    .Select(b => new { b.Title, Price = Math.Round(b.Price, 2) })
    .ToListAsync();
```

## String Methods

| C# | SQL |
|---|---|
| `s.Length` | `LENGTH(s)` |
| `s.ToUpper()` / `s.ToUpperInvariant()` | `UPPER(s)` |
| `s.ToLower()` / `s.ToLowerInvariant()` | `LOWER(s)` |
| `s.Trim()` | `TRIM(s)` |
| `s.TrimStart()` | `LTRIM(s)` |
| `s.TrimEnd()` | `RTRIM(s)` |
| `s.Contains(value)` | `s LIKE '%value%' ESCAPE '\'` |
| `s.StartsWith(value)` | `s LIKE 'value%' ESCAPE '\'` |
| `s.EndsWith(value)` | `s LIKE '%value' ESCAPE '\'` |
| `s.Equals(value)` | `s = value` |
| `s.Replace(old, new)` | `REPLACE(s, old, new)` |
| `s.Substring(start, length)` | `SUBSTR(s, start + 1, length)` |
| `s.IndexOf(value)` | `INSTR(s, value) - 1` |
| `s.LastIndexOf(value)` | `CASE WHEN LENGTH(value) = 0 THEN LENGTH(s) ELSE COALESCE((WITH RECURSIVE find_pos(pos, rem) AS (SELECT 0, s UNION ALL SELECT pos + INSTR(rem, value), SUBSTR(rem, INSTR(rem, value) + 1) FROM find_pos WHERE INSTR(rem, value) > 0) SELECT MAX(pos) - 1 FROM find_pos WHERE pos > 0), -1) END` |
| `s.Insert(index, value)` | `SUBSTR(s, 1, index) \|\| value \|\| SUBSTR(s, index + 1)` |
| `s.Remove(start)` | `SUBSTR(s, 1, start)` |
| `s.Remove(start, count)` | `SUBSTR(s, 1, start) \|\| SUBSTR(s, start + count + 1)` |
| `s.PadLeft(n)` | `CASE WHEN LENGTH(s) >= n THEN s ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB((n - LENGTH(s)) / 2 + (n - LENGTH(s)) % 2)), '00', ' '), 1, n - LENGTH(s)) \|\| s) END` |
| `s.PadLeft(n, c)` | `CASE WHEN LENGTH(s) >= n THEN s ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(n - LENGTH(s))), '00', c), 1, n - LENGTH(s)) \|\| s) END` |
| `s.PadRight(n)` | `CASE WHEN LENGTH(s) >= n THEN s ELSE (s \|\| (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB((n - LENGTH(s)) / 2 + (n - LENGTH(s)) % 2)), '00', ' '), 1, n - LENGTH(s)))) END` |
| `s.PadRight(n, c)` | `CASE WHEN LENGTH(s) >= n THEN s ELSE (s \|\| (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(n - LENGTH(s))), '00', c), 1, n - LENGTH(s)))) END` |
| `s + other` | `s \|\| other` |
| `string.Concat(a, b, ...)` | `a \|\| b \|\| ...` |
| `string.Join(sep, values)` | `val1 \|\| sep \|\| val2 \|\| ...` |
| `string.Compare(a, b)` | `CASE WHEN a = b THEN 0 WHEN a < b THEN -1 ELSE 1 END` |
| `string.IsNullOrEmpty(s)` | `(s IS NULL OR s = '')` |
| `string.IsNullOrWhiteSpace(s)` | `(s IS NULL OR TRIM(s, ' ') = '')` |

Pass `StringComparison.OrdinalIgnoreCase` to `Contains`, `StartsWith`, or `EndsWith` to make the match case insensitive:

```csharp
var results = await db.Table<Book>()
    .Where(b => b.Title.Contains("test", StringComparison.OrdinalIgnoreCase))
    .ToListAsync();
```

The `??` operator translates to `COALESCE`:

```csharp
var results = await db.Table<Book>()
    .Select(b => new { b.Id, Notes = b.Notes ?? "No notes" })
    .ToListAsync();
```

## Char Methods

| C# | SQL |
|---|---|
| `char.ToLower(c)` | `LOWER(c)` |
| `char.ToUpper(c)` | `UPPER(c)` |
| `char.IsWhiteSpace(c)` | `TRIM(c) = ''` |
| `char.IsAsciiDigit(c)` | `c >= '0' AND c <= '9'` |
| `char.IsAsciiLetter(c)` | `(c >= 'a' AND c <= 'z') OR (c >= 'A' AND c <= 'Z')` |
| `char.IsAsciiLetterOrDigit(c)` | `(c >= '0' AND c <= '9') OR (c >= 'a' AND c <= 'z') OR (c >= 'A' AND c <= 'Z')` |
| `char.IsAsciiLetterLower(c)` | `c >= 'a' AND c <= 'z'` |
| `char.IsAsciiLetterUpper(c)` | `c >= 'A' AND c <= 'Z'` |

## DateTime Properties

You can read individual components of a `DateTime` column directly:

```csharp
var recent = await db.Table<Order>()
    .Where(o => o.PlacedAt.Year == 2024 && o.PlacedAt.Month == 12)
    .ToListAsync();

var years = await db.Table<Order>()
    .Select(o => o.PlacedAt.Year)
    .ToListAsync();
```

Supported properties: `Year`, `Month`, `Day`, `Hour`, `Minute`, `Second`, `Millisecond`, `Ticks`, `DayOfWeek`, `DayOfYear`.

## DateTime Methods

```csharp
var shifted = await db.Table<Order>()
    .Select(o => new { o.Id, Due = o.PlacedAt.AddDays(30) })
    .ToListAsync();
```

Supported methods: `Add`, `AddYears`, `AddMonths`, `AddDays`, `AddHours`, `AddMinutes`, `AddSeconds`, `AddMilliseconds`, `AddMicroseconds`, `AddTicks`.

## DateOnly Properties

You can read individual components of a `DateOnly` column the same way as `DateTime`:

```csharp
var recent = await db.Table<Order>()
    .Where(o => o.Date.Year == 2024 && o.Date.Month == 12)
    .ToListAsync();
```

Supported properties: `Year`, `Month`, `Day`, `DayOfWeek`, `DayOfYear`.

## DateOnly Methods

```csharp
var shifted = await db.Table<Order>()
    .Select(o => new { o.Id, Due = o.Date.AddDays(30) })
    .ToListAsync();
```

Supported methods: `AddYears`, `AddMonths`, `AddDays`.

## DateTimeOffset Properties

Supported properties: `Year`, `Month`, `Day`, `Hour`, `Minute`, `Second`, `Millisecond`, `Ticks`, `DayOfWeek`, `DayOfYear`.

## DateTimeOffset Methods

Supported methods: `Add`, `AddYears`, `AddMonths`, `AddDays`, `AddHours`, `AddMinutes`, `AddSeconds`, `AddMilliseconds`, `AddMicroseconds`, `AddTicks`.

## TimeOnly Properties

Supported properties: `Hour`, `Minute`, `Second`, `Ticks`.

## TimeOnly Methods

Supported methods: `Add`, `AddHours`, `AddMinutes`.

## TimeSpan Properties

Supported properties: `Days`, `TotalDays`, `Hours`, `TotalHours`, `Minutes`, `TotalMinutes`, `Seconds`, `TotalSeconds`, `Milliseconds`, `TotalMilliseconds`, `Ticks`.

## TimeSpan Methods

```csharp
var results = await db.Table<Order>()
    .Where(o => o.Duration.Subtract(TimeSpan.FromHours(1)).TotalHours > 5)
    .ToListAsync();
```

Supported methods: `Add`, `Subtract`, `Negate`, `Duration`.

You can also call the static `TimeSpan` creation methods inside an expression:

```csharp
var results = await db.Table<Order>()
    .Where(o => o.Duration == TimeSpan.FromHours(o.Id))
    .ToListAsync();
```

Supported static methods: `FromDays`, `FromHours`, `FromMinutes`, `FromSeconds`, `FromMilliseconds`, `FromMicroseconds`, `FromTicks`.

## Guid

`Guid` columns support equality comparisons:

```csharp
Guid id = Guid.NewGuid();

var result = await db.Table<Order>()
    .Where(o => o.TrackingId == id)
    .FirstOrDefaultAsync();
```
