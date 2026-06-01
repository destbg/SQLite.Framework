# Expressions

LINQ expressions inside `Where`, `Select`, and other methods are translated directly to SQL. The framework stays close to the shape of the LINQ query and does not wrap the result in an extra subquery just to make a method work. When a method does not have a clean SQL equivalent, you get a clear `NotSupportedException` rather than a hidden rewrite. When SQLite has no built-in for a method but the same shape can be expressed inline (often as a `CASE WHEN ...` block), the framework uses that. This page covers what is supported beyond basic comparisons.

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

Integer arithmetic is computed in 64-bit and then narrowed back to the result type with the same wrap-around as unchecked C# code. For example, an `int * int` product that exceeds `int.MaxValue` wraps the same way it does in .NET.

Division or modulo by zero follows SQLite, not .NET. `x / 0` and `x % 0` evaluate to `NULL`, which materializes as `0` in a non-nullable projection or as `null` in a nullable one, rather than throwing `DivideByZeroException`. This also applies to floating-point division by zero, so `5.0 / 0.0` is `NULL`, not infinity.

Floating-point operations that produce `NaN` or infinity in .NET return `NULL` in SQLite, because SQLite cannot store those values. This includes `Math.Sqrt` of a negative number, `Math.Log` of zero or a negative number, and `Math.Acos` of a value outside the range from -1 to 1.

## Math Functions

| C# | SQL |
|---|---|
| `Math.Abs(x)` | `ABS(x)` |
| `Math.Round(x)` | round half to even (banker's), the .NET default, via a `CASE` over `ROUND` |
| `Math.Round(x, digits)` | round half to even (banker's), the .NET default, via a `CASE` over `ROUND` |
| `Math.Round(x, MidpointRounding.AwayFromZero)` | `ROUND(x)` |
| `Math.Round(x, MidpointRounding.ToEven)` | round half to even (banker's), via a `CASE` over `ROUND` |
| `Math.Round(x, digits, MidpointRounding.AwayFromZero)` | `ROUND(x, digits)` |
| `Math.Round(x, digits, MidpointRounding.ToEven)` | round half to even (banker's), via a `CASE` over `ROUND` |
| `Math.Floor(x)` | `FLOOR(x)` |
| `Math.Ceiling(x)` | `CEIL(x)` |
| `Math.Truncate(x)` | `TRUNC(x)` |
| `Math.Pow(x, y)` | `POWER(CAST(x AS REAL), y)` |
| `Math.Sqrt(x)` | `SQRT(x)` |
| `Math.Cbrt(x)` | `CASE WHEN x >= 0 THEN POWER(x, 1.0/3.0) ELSE -POWER(-x, 1.0/3.0) END` |
| `Math.Exp(x)` | `EXP(x)` |
| `Math.Log(x)` | `LOG(x)` |
| `Math.Log(x, base)` | `LOG(x) / LOG(base)` |
| `Math.Log10(x)` | `LOG10(x)` |
| `Math.Log2(x)` | `LOG2(x)` |
| `Math.Sign(x)` | `CASE WHEN x > 0 THEN 1 WHEN x < 0 THEN -1 ELSE 0 END` |
| `Math.Max(x, y)` | `CASE WHEN x > y THEN x ELSE y END` |
| `Math.Min(x, y)` | `CASE WHEN x < y THEN x ELSE y END` |
| `Math.Clamp(x, min, max)` | `CASE WHEN x < min THEN min WHEN x > max THEN max ELSE x END` |
| `Math.Sin(x)` | `SIN(x)` |
| `Math.Cos(x)` | `COS(x)` |
| `Math.Tan(x)` | `TAN(x)` |
| `Math.Asin(x)` | `ASIN(x)` |
| `Math.Acos(x)` | `ACOS(x)` |
| `Math.Atan(x)` | `ATAN(x)` |
| `Math.Atan2(y, x)` | `ATAN2(y, x)` |
| `Math.Sinh(x)` | `SINH(x)` |
| `Math.Cosh(x)` | `COSH(x)` |
| `Math.Tanh(x)` | `TANH(x)` |
| `Math.Asinh(x)` | `ASINH(x)` |
| `Math.Acosh(x)` | `ACOSH(x)` |
| `Math.Atanh(x)` | `ATANH(x)` |

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
| `s[index]` | `SUBSTR(s, index + 1, 1)` |
| `s.IndexOf(value)` | `INSTR(s, value) - 1` |
| `s.IndexOf(value, startIndex)` | `INSTR(SUBSTR(s, startIndex + 1), value)` adjusted back to a 0-based absolute index, or `-1` |
| `s.LastIndexOf(value)` | `CASE WHEN LENGTH(value) = 0 THEN LENGTH(s) ELSE COALESCE((WITH RECURSIVE find_pos(pos, rem) AS (SELECT 0, s UNION ALL SELECT pos + INSTR(rem, value), SUBSTR(rem, INSTR(rem, value) + 1) FROM find_pos WHERE INSTR(rem, value) > 0) SELECT MAX(pos) - 1 FROM find_pos WHERE pos > 0), -1) END` |
| `s.Insert(index, value)` | `SUBSTR(s, 1, index) \|\| value \|\| SUBSTR(s, index + 1)` |
| `s.Remove(start)` | `SUBSTR(s, 1, start)` |
| `s.Remove(start, count)` | `SUBSTR(s, 1, start) \|\| SUBSTR(s, start + count + 1)` |
| `s.PadLeft(n)` | `CASE WHEN LENGTH(s) >= n THEN s ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(n - LENGTH(s))), '00', ' '), 1, n - LENGTH(s)) \|\| s) END` |
| `s.PadLeft(n, c)` | `CASE WHEN LENGTH(s) >= n THEN s ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(n - LENGTH(s))), '00', c), 1, n - LENGTH(s)) \|\| s) END` |
| `s.PadRight(n)` | `CASE WHEN LENGTH(s) >= n THEN s ELSE (s \|\| (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(n - LENGTH(s))), '00', ' '), 1, n - LENGTH(s)))) END` |
| `s.PadRight(n, c)` | `CASE WHEN LENGTH(s) >= n THEN s ELSE (s \|\| (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(n - LENGTH(s))), '00', c), 1, n - LENGTH(s)))) END` |
| `s + other` | `s \|\| other` |
| `string.Concat(a, b, ...)` | `a \|\| b \|\| ...` |
| `string.Join(sep, values)` | `val1 \|\| sep \|\| val2 \|\| ...` |
| `string.Compare(a, b)` | `CASE WHEN a = b THEN 0 WHEN a < b THEN -1 ELSE 1 END` |
| `s.CompareTo(other)` | `CASE WHEN s = other THEN 0 WHEN s < other THEN -1 ELSE 1 END` |
| `string.IsNullOrEmpty(s)` | `(s IS NULL OR s = '')` |
| `string.IsNullOrWhiteSpace(s)` | `(s IS NULL OR TRIM(s, CHAR(9, 10, 11, 12, 13, 32)) = '')` |

`s.Length` maps to SQLite `LENGTH`, which counts Unicode code points. .NET `string.Length` counts UTF-16 code units, so the two differ for characters outside the Basic Multilingual Plane such as emoji, where `LENGTH` returns 1 but `string.Length` returns 2.

In `+`, `string.Concat`, and `string.Join`, a nullable string column is wrapped in `COALESCE(col, '')`, so a `NULL` value becomes an empty string. This matches .NET, where `string.Concat` and `string.Join` treat a `null` argument as empty.

String comparison and ordering use SQLite's default `BINARY` collation, which compares by byte value (ordinal). .NET `OrderBy`, `string.Compare`, and `s.CompareTo` use culture-sensitive comparison by default. The two can order strings differently when case or non-ASCII characters are involved. For example, `"a".CompareTo("B")` is negative in .NET but positive under `BINARY`, because lowercase letters sort after uppercase ones by byte value.

`Contains`, `StartsWith`, and `EndsWith` use `LIKE`, which is case-insensitive for ASCII by default. To make them case-sensitive, build the database with `UseCaseSensitiveStringComparison()`. They then translate to `INSTR` / `SUBSTR` instead of `LIKE`. See [Storage Options](Storage%20Options).

Pass `StringComparison.OrdinalIgnoreCase` to `Contains`, `StartsWith`, or `EndsWith` to force a case-insensitive match regardless of that option:

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
| `char.IsWhiteSpace(c)` | `TRIM(c, CHAR(9, 10, 11, 12, 13, 32)) = ''` |
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

Supported properties: `Year`, `Month`, `Day`, `Hour`, `Minute`, `Second`, `Millisecond`, `Ticks`, `DayOfWeek`, `DayOfYear`, `Date`, `TimeOfDay`.

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

Supported properties: `Year`, `Month`, `Day`, `Hour`, `Minute`, `Second`, `Millisecond`, `Ticks`, `DayOfWeek`, `DayOfYear`, `Date`, `TimeOfDay`.

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
