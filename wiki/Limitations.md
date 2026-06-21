# Limitations

Where query behavior differs from LINQ-to-Objects. See [Storage Options](Storage%20Options) for the storage modes referenced below.

## Numbers

- Divide or modulo by zero is `NULL` (reads back `0` for a non-nullable result, `null` for a nullable one).
- A math call whose .NET result is `NaN` reads back as `null`, for example `Math.Sqrt(-1)`, `Math.Acos(2)` or `Math.Pow(-2, 0.5)`. A result that is infinity, such as `Math.Exp(1000)` or `Math.Atanh(1)`, comes back correct.
- `Math.Log`, `Math.Log2` and `Math.Log10` of zero read back as `null` where .NET returns negative infinity. `Math.Log(a, base)` reads back as `null` when the value is zero or the base is 0, 1 or negative.
- `Math.Cbrt` is computed through `POWER`, so the result can differ from .NET in the last bits. An exact cube such as `Math.Cbrt(64)` can read back just below the exact root.
- Float `ToString()` keeps at most 15 significant digits, prints a value at or above 1e15 in scientific notation, prints negative zero as `"0"`, and prints infinity as `"INF"`.
- `decimal` is not exact: `Real` storage is a 64-bit float, `Text` storage casts to float for compare and order.
- On `Real` decimal storage, `ToString()` formats like a `double`: trailing zeros such as `10.50` are dropped, and a very small or very large value prints in scientific notation. `Text` storage returns the stored .NET string.
- On `Text` decimal storage, `Distinct`, the set operators, and a subquery `Contains` compare the stored text. Two equal values with a different scale, such as `10.0` and `10.00`, are then treated as different.
- `float` math runs in 64-bit precision, so a `float` result can differ from .NET in the last digits, and `ToString()` on a fractional `float` prints the digits of the stored 64-bit value. SQLite has no 32-bit float type.
- Integer overflow throws `OverflowException`. A `Sum` past 64 bits throws `SQLiteException`, and `Average` stays finite where .NET would throw.
- `uint` and `ulong` arithmetic wraps while the result fits 64 bits, then throws.
- A `uint` multiplication keeps the full 64-bit product instead of the 32-bit wrapped value, both when widened (`(long)(a * b)`) and when used directly (`a * b == 0u`).
- `.Equals` compares by value, so `intColumn.Equals(5L)` is `true` in SQL but `false` in .NET, where `object.Equals` on two different boxed numeric types is always false.
- `Math.Round` with `AwayFromZero` can differ in the last digit.
- `NaN` does not round-trip (stored as `NULL`). Infinity is fine.
- `Parse` and a narrowing cast of an integer column map to `CAST` and do not validate or throw. A narrowing cast of a floating-point column to a smaller integer type throws `OverflowException` when the value is out of the target range, where .NET would saturate or wrap.
- A cast of a floating-point value to `uint` or `ulong` does not throw and does not limit the value to the type range. A negative value, or a value above the range, wraps to a different number, where .NET clamps it to the nearest valid value.
- Only the single-string `int.Parse`/`double.Parse` maps to `CAST`. The `NumberStyles`/`IFormatProvider` overloads (such as hex parsing) run in memory in a `Select` and throw in a `Where`.
- `Convert.ToInt32` and `Convert.ToInt64` of a `double` or `float` round half away from zero, where .NET rounds half to even, so a value such as `2.5` reads back as `3` instead of `2`. The other `Convert` methods run in memory in a `Select` and throw in a `Where`.
- `Convert.ToInt64` of a floating-point value above the `long` range, such as `Convert.ToInt64(1e19)`, returns the largest or smallest `long` instead of throwing `OverflowException`. `Convert.ToInt32` still throws, since the result is read into a smaller type.
- `Math.Clamp` with `min` greater than `max` returns `min` instead of throwing.
- A cast of a floating-point value above the `decimal` range to `decimal`, such as `(decimal)1e30`, returns the largest or smallest `decimal` instead of throwing `OverflowException`.
- `Math.Abs(long.MinValue)` throws a `SQLiteException`, since its result does not fit a signed 64-bit integer.
- The bitwise complement `~` of a native integer (`nint` or `nuint`) is not supported.

## Strings

- `Length` counts code points, and `PadLeft`/`PadRight` measure the target width the same way.
- Ordering and comparison use byte value (`BINARY`), so `"B"` sorts before `"a"`.
- `Substring`, `Remove`, `Insert`, `IndexOf` and `LastIndexOf` clamp out-of-range arguments instead of throwing.
- `Replace("", ...)` returns the original string.
- `ToUpper` and `ToLower`, on both `string` and `char`, fold only ASCII unless the SQLite build has ICU.
- The `CultureInfo` overloads of `ToUpper` and `ToLower`, on both `string` and `char` throw in a `Where`.
- Case-insensitive `Equals`, `Compare`, `Contains`, `StartsWith` and `EndsWith` (`OrdinalIgnoreCase`) also fold only ASCII.
- Concatenating a non-string column keeps its stored form (`bool` to `1`/`0`, `enum` to its number, `DateTime` to ticks or text).
- A `char` taken from a string can be half of a character that needs two slots in .NET, such as an emoji. SQLite stores whole characters only, so reading that half on its own does not come back the same and can throw.
- `Enum.Parse` strips ASCII whitespace anywhere in the string, so the spaced `[Flags]` form like `"Read, Write"` parses but a name with embedded whitespace like `"News\tpaper"` matches `"Newspaper"` where .NET would throw.
- When an enum is stored as Text, `ToString("D")` and `ToString("X")` return the stored member name for a value that is not one single defined member. A `[Flags]` combination such as `Read, Write`, or an undefined number, reads back as the stored text instead of the number or hex string.

## Ordering and set operations

- Chained `OrderBy` keeps only the last key, so a second `OrderBy` drops the first key.
- `OrderBy(...).Select(...).Distinct()` returns its results in an undefined order when the `Select` drops the column that `OrderBy` sorted on, for example `OrderBy(x => x.Date).Select(x => x.Name).Distinct()`.
- `Union`, `Distinct`, `Intersect` and `Except` dedup by value, not by reference.
- `Union`, `Intersect` and `Except` return rows in sorted order, not the first-appearance order that LINQ-to-Objects keeps. `Concat` keeps first-appearance order.
- Without an explicit `OrderBy`, row order follows SQLite's query plan rather than insertion order. An index over the read column makes `Distinct` return its values in sorted order, and makes `First`, `FirstOrDefault`, `Single`, `ElementAt` and `Take` read the lowest indexed rows instead of the first inserted ones.
- `Union`, `Intersect` and `Except` over a `ulong` column sort by the signed stored value, so a value at or above 2^63 sorts before a smaller value.
- `GroupBy` returns groups in key order, not the first-seen order that LINQ-to-Objects uses.
- `Reverse` on one side of a `Union`, `Concat`, `Except` or `Intersect` does not take effect, because SQLite has no row order to flip inside a combined query.
- `string.Join` over a query whose last step is `Reverse` is not supported and throws.

## Query operators

- Some LINQ operators are not translated to SQL and throw `NotSupportedException` on a table query. These are `Last`, `LastOrDefault`, `Order`, `OrderDescending`, `MaxBy`, `MinBy`, `DistinctBy`, `SkipLast`, `TakeLast`, `Append`, `Prepend`, `Chunk`, `ExceptBy`, `UnionBy`, `IntersectBy`, `SkipWhile` and `TakeWhile`.

## Joins and SelectMany

- A correlated subquery used directly as a second `from` source, for example `from a in db.Table<Author>() from b in db.Table<Book>().Where(b => b.AuthorId == a.Id)`, is not supported, since SQLite has no `LATERAL` join.
- In a recursive common table expression, a positional constructor whose parameter order differs from the entity's property order lines the columns up wrong.

## Null comparisons

- `>`, `<`, `>=`, `<=` on a `NULL` column are `NULL`: the row drops in `Where`/`All`, reads as `false` in `ToList`, and throws in `First`/`Single`. Equality stays correct via `IS`.
- Reading `.Value` on a `NULL` nullable column returns the type default instead of throwing `InvalidOperationException`.
- A projected entity reads back as `null` when all of its mapped columns are `NULL`, so a row whose values are all null cannot be told apart from a missing outer-join row.

## Aggregates

- A grouped `Min`, `Max` or `Average` over a per-group filter that matches no rows returns the type default instead of throwing. `Sum` returns `0`, the same as LINQ.
- A window `Max`, `Min` or `Average` over a `ulong` column is not correct for values at or above 2^63, since the value is stored as a signed integer. A window `Average` over a `uint` column is exact.
- A window `Sum` that sees no rows, because the frame is empty or a `Filter` removes every row, reads back as `NULL`, not `0`.

## Dates, times and storage

- `AddMonths` and `AddYears` whose result lands in December of year 9999 return the default date, since the date math overflows past SQLite's maximum date.
- `AddSeconds`, `AddMinutes`, `AddHours`, `AddDays`, `AddMilliseconds` and the other `Add` methods that take a fractional amount can land one tick away from the .NET result. SQLite multiplies the amount by the tick scale in one floating-point step, while .NET reaches the tick count through a different intermediate unit, so the last tick can round the other way.
- `DateTimeOffset` drops its offset.
- A `DateTime` stored as `Integer` or `Text` ticks reads back with `Kind` set to `Unspecified`, since the tick count carries no kind.
- Date and time component access (`.Year`, `.Day`, `.Days`, ...) in `Where`/`OrderBy` needs `Integer` or `Ticks` storage.
- A value stored as `Text` compares and orders by the stored string, not by its value. This covers `enum`, `TimeSpan`, `DateOnly`, `TimeOnly`, `DateTime` and `decimal`, and the `HasFlag`, bitwise, comparison and cast operators on a `Text`-stored enum.

## R-Tree

- On the default `Float` storage, coordinates are stored as 32-bit floats. A value above 2^24, or a fractional value that a 32-bit float cannot hold exactly such as `0.2`, loses precision and can miss an exact boundary match.

## Functions and JSON collections

- `SQLiteFunctions.Min` and `Max` need two or more arguments.
- On a JSON array, `ElementAt` past the end, `First`, `Last` or `Single` over an empty array, `Single` over two or more elements, and `Min`/`Max`/`Average`/`Sum` over an empty array all return the type default instead of throwing.
- On a JSON array that holds a `null` element, `Except` and `Intersect` against another list that also holds `null` drop the rows that SQL `NOT IN` and `IN` cannot decide through `NULL`, and `Distinct().Count()` leaves the `null` out of the count.
- A JSON list of `double` cannot store `NaN`, `+Infinity` or `-Infinity`. JSON has no way to write these values, so adding a list that holds one fails.
- `DateTime` values inside a JSON list are kept as text. Reading a part like `.Year`, or comparing them, follows the same rules as `Text` date storage, not .NET, so results can differ.
- `Skip` and `Take` on a JSON list take a fixed number or a value from a local variable, not a column of the outer row.
- `GetRange` on a JSON list that asks for more items than are there returns the items that fit, instead of throwing.
- Projecting a JSON dictionary's `Keys` or `Values` collection on its own is not supported.
- Building a new collection from a JSON list with `ToArray` or `ToHashSet` is not supported.
- `OrderBy` followed by `Reverse` on a JSON list keeps rows that share the same sort key in their first-seen order, not the reversed order that LINQ-to-Objects gives, because the reverse is done by sorting the other way.
- On a JSON dictionary, `ContainsKey` and the indexer work in a `Where` or `OrderBy` only with a constant key. A key taken from a column or variable, and `Dictionary.Contains` of a whole key-value pair, are not supported there.
- On a JSON dictionary, the indexer for a key that is not present returns the type default instead of throwing.

## Binary data

- A `byte[]` column supports `Length` and value equality (`==` and `SequenceEqual`) in a query. Reading a single byte by index, and `Contains` of a single byte, are not supported in a query.

## Custom converters

- A `bool` column whose converter stores a non-numeric value, such as the text `yes`/`no`, does not work when used directly as a condition, for example `Where(r => r.Flag)` or `r.Flag && other`. SQLite reads the stored text as the number `0`, so the condition is always false.

## Full text search

- On an external-content FTS5 table, reading an indexed column value works only when the content table's column has the same name as the indexed property. A column renamed with `[Column]` can still be matched, but its value cannot be read back.

## Projections

- A projection that builds an object (`Select(r => new Dto { ... })`) binds public properties only. Public fields are left at their default value.
- Calling `GetType` on a value that is `null` throws a different error than LINQ-to-Objects.

## Writes

- An `Upsert` that inserts a row writes the new auto-increment key back to the object only when the new row id differs from the last inserted row id on the connection. An earlier insert, even into another table, that already left the same id stops the write-back.
- An `Upsert` with a `DoUpdate` action always writes the object's value for every column, even one left at its CLR default that has a database `DEFAULT`. This is needed so a conflict updates the row to the incoming value through `excluded`. So a fresh insert through `DoUpdate` stores the CLR default rather than the database default, unlike `Add`, `AddOrUpdate` or an `Upsert` with `DoNothing`.

## Raw SQL

- Two `FromSql` fragments composed in the same query that use the same parameter name share one bound value, so the last value wins.
