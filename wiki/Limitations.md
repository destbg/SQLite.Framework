# Limitations

Where query behavior differs from LINQ-to-Objects. See [Storage Options](Storage%20Options) for the storage modes referenced below.

## Numbers

- Divide or modulo by zero is `NULL` (reads back `0` for a non-nullable result, `null` for a nullable one).
- `Math.Sqrt`, `Math.Log` and `Math.Acos` out of domain are `NULL`. SQLite has no `NaN` or infinity.
- Float `ToString()` keeps the decimal point, so `1.0` becomes `"1.0"`.
- `decimal` is not exact: `Real` storage is a 64-bit float, `Text` storage casts to float for compare and order.
- On `Text` decimal storage, `Distinct`, the set operators, and a subquery `Contains` compare the stored text. Two equal values with a different scale, such as `10.0` and `10.00`, are then treated as different.
- `float` math runs in 64-bit precision, so a `float` result can differ from .NET in the last digits. SQLite has no 32-bit float type.
- Integer overflow throws `OverflowException`. A `Sum` past 64 bits throws `SQLiteException`, and `Average` stays finite where .NET would throw.
- `uint` and `ulong` arithmetic wraps while the result fits 64 bits, then throws.
- A `uint` multiplication that is then widened to a larger type, such as `(long)(a * b)`, keeps the full 64-bit product instead of the 32-bit wrapped value. Read the result back as `uint` to get the wrapped value.
- `.Equals` compares by value, so `intColumn.Equals(5L)` is `true` in SQL but `false` in .NET, where `object.Equals` on two different boxed numeric types is always false.
- `Math.Round` with `AwayFromZero` can differ in the last digit.
- `NaN` does not round-trip (stored as `NULL`). Infinity is fine.
- `Parse` and a narrowing cast of an integer column map to `CAST` and do not validate or throw. A narrowing cast of a floating-point column to a smaller integer type throws `OverflowException` when the value is out of the target range, where .NET would saturate or wrap.
- Only the single-string `int.Parse`/`double.Parse` maps to `CAST`. The `NumberStyles`/`IFormatProvider` overloads (such as hex parsing) run in memory in a `Select` and throw in a `Where`.
- `Math.Clamp` with `min` greater than `max` returns `min` instead of throwing.
- `Math.Abs(long.MinValue)` throws a `SQLiteException`, since its result does not fit a signed 64-bit integer.

## Strings

- `Length` counts code points, and `PadLeft`/`PadRight` measure the target width the same way.
- Ordering and comparison use byte value (`BINARY`), so `"B"` sorts before `"a"`.
- `Substring`, `Remove`, `Insert`, `IndexOf` and `LastIndexOf` clamp out-of-range arguments instead of throwing.
- `Replace("", ...)` returns the original string.
- `ToUpper` and `ToLower` fold only ASCII unless the SQLite build has ICU.
- Case-insensitive `Equals` and `Compare` (`OrdinalIgnoreCase`) also fold only ASCII.
- Concatenating a non-string column keeps its stored form (`bool` to `1`/`0`, `enum` to its number, `DateTime` to ticks or text), matching EF Core.
- `Enum.Parse` strips ASCII whitespace anywhere in the string, so the spaced `[Flags]` form like `"Read, Write"` parses but a name with embedded whitespace like `"News\tpaper"` matches `"Newspaper"` where .NET would throw.

## Ordering and set operations

- Chained `OrderBy` keeps only the last key, like EF Core. Use `ThenBy` to keep both.
- `Union`, `Distinct`, `Intersect` and `Except` dedup by value, not by reference.
- `Union`, `Intersect` and `Except` return rows in sorted order, not the first-appearance order that LINQ-to-Objects keeps. `Distinct` and `Concat` do keep first-appearance order.
- `Union`, `Intersect` and `Except` over a `ulong` column sort by the signed stored value, so a value at or above 2^63 sorts before a smaller value.
- `GroupBy` returns groups in key order, not the first-seen order that LINQ-to-Objects uses.

## Joins and SelectMany

- A correlated subquery used directly as a second `from` source, for example `from a in db.Table<Author>() from b in db.Table<Book>().Where(b => b.AuthorId == a.Id)`, is not supported, since SQLite has no `LATERAL` join. Put the correlation in a `where` after the join instead.
- In a recursive common table expression, write the recursive term as a member initializer such as `new T { A = ..., B = ... }`, or pass constructor arguments in the same order as the entity's properties. A positional constructor whose parameter order differs from the property order lines the columns up wrong.

## Null comparisons

- `>`, `<`, `>=`, `<=` on a `NULL` column are `NULL`: the row drops in `Where`/`All`, reads as `false` in `ToList`, and throws in `First`/`Single`. Equality stays correct via `IS`.
- Reading `.Value` on a `NULL` nullable column returns the type default instead of throwing `InvalidOperationException`.
- A projected entity reads back as `null` when all of its mapped columns are `NULL`, so a row whose values are all null cannot be told apart from a missing outer-join row.

## Aggregates

- A grouped `Min`, `Max` or `Average` over a per-group filter that matches no rows returns the type default instead of throwing. `Sum` returns `0`, the same as LINQ.
- A window `Max`, `Min` or `Average` over a `ulong` column is not correct for values at or above 2^63, since the value is stored as a signed integer. A window `Average` over a `uint` column is exact.

## Dates, times and storage

- `AddMonths` and `AddYears` whose result lands in December of year 9999 return the default date, since the date math overflows past SQLite's maximum date.
- `DateTimeOffset` drops its offset.
- Date and time component access (`.Year`, `.Day`, `.Days`, ...) in `Where`/`OrderBy` needs `Integer` or `Ticks` storage.
- A value stored as `Text` compares and orders by the stored string, not by its value. This covers `enum`, `TimeSpan`, `DateOnly`, `TimeOnly`, `DateTime` and `decimal`, and the `HasFlag`, bitwise, comparison and cast operators on a `Text`-stored enum.

## R-Tree

- On the default `Float` storage, coordinates are stored as 32-bit floats. A value above 2^24, or a fractional value that a 32-bit float cannot hold exactly such as `0.2`, loses precision and can miss an exact boundary match. Use `SQLiteRTreeStorage.Int32` for exact integer coordinates.

## Functions and JSON collections

- `SQLiteFunctions.Min` and `Max` need two or more arguments.
- On a JSON array, `ElementAt` past the end, `First`, `Last` or `Single` over an empty array, `Single` over two or more elements, and `Min`/`Max`/`Average`/`Sum` over an empty array all return the type default instead of throwing.
- On a JSON array that holds a `null` element, `Except` and `Intersect` against another list that also holds `null` drop the rows that SQL `NOT IN` and `IN` cannot decide through `NULL`, and `Distinct().Count()` leaves the `null` out of the count.

## Raw SQL

- Two `FromSql` fragments composed in the same query that use the same parameter name share one bound value, so the last value wins. Give each fragment its own parameter names.
