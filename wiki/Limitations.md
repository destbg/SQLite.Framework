# Limitations

The framework translates LINQ to SQL and runs it in SQLite. SQLite and .NET do not agree on every detail, so a few queries return a different result than the same code would return in memory with LINQ to Objects. This page collects those cases. Most of them come from SQLite semantics that cannot be reproduced in SQL without a large cost, so the framework follows SQLite.

## Numbers

**Division and modulo by zero return NULL.** `x / 0` and `x % 0` evaluate to `NULL`, not a `DivideByZeroException`. The `NULL` materializes as `0` in a non-nullable projection or as `null` in a nullable one. This also applies to floating-point division, so `5.0 / 0.0` is `NULL`, not infinity.

**Floating-point domain errors return NULL.** Operations that produce `NaN` or infinity in .NET return `NULL` in SQLite, because SQLite cannot store those values. This includes `Math.Sqrt` of a negative number, `Math.Log` of zero or a negative number, and `Math.Acos` of a value outside the range from -1 to 1.

**`double.ToString()` formatting differs.** A floating-point `ToString()` maps to `CAST(x AS TEXT)`, which formats differently from .NET. For example `1.0` becomes `"1.0"` in SQLite but `"1"` in .NET.

**`decimal` is not exact.** SQLite has no decimal type. With the default `Real` storage a `decimal` is kept as a 64-bit float and loses precision past about 15 digits. With `Text` storage the full value is stored, but comparisons and ordering still cast to `REAL` and lose precision inside the query. For exact storage use `Text`. See [Storage Options](Storage%20Options).

Integer arithmetic does match .NET. It is computed in 64-bit and narrowed back to the result type with the same wrap-around as unchecked C# code, so an `int * int` product that exceeds `int.MaxValue` wraps instead of throwing.

## Strings

**Length counts code points.** `s.Length` maps to `LENGTH`, which counts Unicode code points. .NET `string.Length` counts UTF-16 code units. The two differ for characters outside the Basic Multilingual Plane, such as emoji, where `LENGTH` returns 1 but `string.Length` returns 2.

**Ordering and comparison are binary.** `OrderBy`, `string.Compare`, `s.CompareTo`, and the `<` and `>` operators use SQLite's default `BINARY` collation, which compares by byte value (ordinal). .NET uses culture-sensitive comparison by default. The two can order strings differently when case or non-ASCII characters are involved. For example `"a".CompareTo("B")` is negative in .NET but positive under `BINARY`, because lowercase letters sort after uppercase ones by byte value.

**Substring clamps instead of throwing.** `s.Substring(start, length)` maps to `SUBSTR`, which clamps out-of-range arguments. `"ab".Substring(0, 5)` returns `"ab"` and `"ab".Substring(5)` returns `""`. .NET throws `ArgumentOutOfRangeException` in both cases. `s.Remove` clamps the same way.

## Null comparisons

**Order comparisons on a nullable column follow three-valued logic.** `>`, `<`, `>=`, and `<=` are `NULL` when one side is a `NULL` column, while the same lifted comparison in .NET returns `false`. In a `Where` clause and in `All`, the result still matches .NET, because the `NULL` row is excluded or counted as failing. When the comparison is projected as a value, a list (`ToList`) materializes the `NULL` as the default `false`, which matches .NET, but reading it as a single scalar with `First` or `Single` throws, because a `NULL` cannot be read into a non-nullable `bool`. Use `ToList`, or project to `bool?`, to avoid the throw. Equality (`==` and `!=`) is not affected, because it translates to the null-safe `IS` and `IS NOT`.

## Dates and times

**`DateTimeOffset` does not keep its offset.** It is stored as ticks, or as formatted text, without the offset. If you need the original offset, store it in a separate column. See [Storage Options](Storage%20Options).

**Component access needs the numeric storage mode.** Property access like `.Year`, `.Month`, `.Day`, `.Hour`, `.Days`, and `.TotalHours` in `Where` and `OrderBy` works only when the value is stored as `Integer` or `Ticks`. Under `TextFormatted` or `Text` storage these properties work only in `Select`, where the value is read first and the property is computed in C#. See [Storage Options](Storage%20Options).

## SQLite functions

**`SQLiteFunctions.Min` and `SQLiteFunctions.Max` need two or more arguments.** Called with a single argument they compile, but SQLite reads `min(x)` and `max(x)` as the aggregate forms, so the query silently turns into an aggregate and returns one row instead of one per input row. For an aggregate over a column, use LINQ's own `Min` and `Max`. See [SQLite Functions](SQLite%20Functions).
