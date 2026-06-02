# Limitations

Query behavior that is easy to miss.

## Numbers

**Divide or modulo by zero is `NULL`.** `x / 0` and `x % 0` (including `5.0 / 0.0`) give `NULL`, which reads back as `0` in a non-nullable projection and `null` in a nullable one.

**Some float operations are `NULL`.** `Math.Sqrt` of a negative, `Math.Log` of zero or a negative, and `Math.Acos` out of range give `NULL`, since SQLite has no `NaN` or infinity.

**Float `ToString()` keeps a decimal point.** It maps to `CAST(x AS TEXT)`, so `1.0` becomes `"1.0"`.

**`decimal` is not exact.** `Real` storage keeps it as a 64-bit float (about 15 digits). `Text` storage keeps the full value but casts to float for comparison and ordering. See [Storage Options](Storage%20Options).

**Integer overflow throws.** Math runs in 64-bit and is read back into the result type. A value that does not fit throws `OverflowException`, for example an `int * int` product or a `Sum` past `int.MaxValue`. Cast wider, like `(long)a * b`.

**`NaN` does not round-trip.** A `double` or `float` `NaN` is stored as `NULL`. A nullable column reads back `null`, and a non-nullable column fails with a `NOT NULL` error. Infinity is fine.

**`Parse` over a column maps to `CAST`.** `int.Parse`, `double.Parse`, `Enum.Parse` and similar do not validate. Bad input reads as `0` or keeps the numeric prefix (`"12abc"` becomes `12`), and an out-of-range value clamps to the 64-bit limits. .NET would throw `FormatException`. A constant argument is parsed in memory and stays correct.

## Strings

**`Length` counts code points.** A non-BMP character such as an emoji counts as 1.

**Ordering and comparison are by byte value.** `OrderBy`, `string.Compare`, `CompareTo`, `<` and `>` use SQLite `BINARY`, so `"B"` sorts before `"a"`.

**`Substring` and `Remove` clamp.** Out-of-range arguments are clamped, not rejected. `"ab".Substring(0, 5)` is `"ab"` and `"ab".Substring(5)` is `""`. A negative length maps to `SUBSTR`, which takes a leftward window, so `"hello".Substring(1, -3)` is `"h"` where .NET throws.

**`Replace("", ...)` returns the original.** SQLite `REPLACE` ignores an empty search string, where .NET throws `ArgumentException`.

**`ToUpper` and `ToLower` depend on the SQLite build.** The default build folds only `a` to `z` and `A` to `Z`, so non-ASCII letters are left as is. An ICU build folds more.

## Null comparisons

**Order comparisons on a `NULL` column are `NULL`.** `>`, `<`, `>=`, `<=` give `NULL` when one side is a `NULL` column. A `Where` or `All` drops the row, `ToList` reads it as `false`, and `First` or `Single` throws because a `NULL` cannot read into `bool`. Use `bool?` or `ToList`. Equality (`==`, `!=`) stays correct via `IS`.

## Dates and times

**`DateTimeOffset` drops its offset.** It is stored as ticks or text without the offset. Keep the offset in its own column. See [Storage Options](Storage%20Options).

**Component access needs numeric storage.** `.Year`, `.Month`, `.Day`, `.Hour`, `.Days`, `.TotalHours` in `Where` and `OrderBy` need `Integer` or `Ticks` storage. Under `TextFormatted` or `Text` they work only in `Select`, computed in memory. See [Storage Options](Storage%20Options).

## SQLite functions

**`SQLiteFunctions.Min` and `Max` need two or more arguments.** With one argument SQLite reads `min(x)` and `max(x)` as aggregates, so the query returns one row. To aggregate a column use LINQ `Min` and `Max`. See [SQLite Functions](SQLite%20Functions).

## JSON-stored collections

**`ElementAt` past the end returns the default.** Indexing runs as a subquery, so an out-of-range index returns the type default like `ElementAtOrDefault`, not `ArgumentOutOfRangeException`.
