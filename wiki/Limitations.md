# Limitations

Where query behavior differs from LINQ-to-Objects. See [Storage Options](Storage%20Options) for the storage modes referenced below.

## Numbers

- Divide or modulo by zero is `NULL` (reads back `0` for a non-nullable result, `null` for a nullable one).
- `Math.Sqrt`, `Math.Log` and `Math.Acos` out of domain are `NULL`. SQLite has no `NaN` or infinity.
- Float `ToString()` keeps the decimal point, so `1.0` becomes `"1.0"`.
- `decimal` is not exact: `Real` storage is a 64-bit float, `Text` storage casts to float for compare and order.
- `float` math runs in 64-bit precision, so a `float` result can differ from .NET in the last digits. SQLite has no 32-bit float type.
- Integer overflow throws `OverflowException`. A `Sum` past 64 bits throws `SQLiteException`, and `Average` stays finite where .NET would throw.
- `uint` and `ulong` arithmetic wraps while the result fits 64 bits, then throws.
- `.Equals` compares by value, so `intColumn.Equals(5L)` is `true` in SQL but `false` in .NET, where `object.Equals` on two different boxed numeric types is always false.
- `Math.Round` with `AwayFromZero` can differ in the last digit.
- `NaN` does not round-trip (stored as `NULL`). Infinity is fine.
- `Parse` and narrowing casts over a column map to `CAST` and do not validate or throw.
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

## Ordering and set operations

- Chained `OrderBy` keeps only the last key, like EF Core. Use `ThenBy` to keep both.
- `Union`, `Distinct`, `Intersect` and `Except` dedup by value, not by reference.
- `GroupBy` returns groups in key order, not the first-seen order that LINQ-to-Objects uses.

## Null comparisons

- `>`, `<`, `>=`, `<=` on a `NULL` column are `NULL`: the row drops in `Where`/`All`, reads as `false` in `ToList`, and throws in `First`/`Single`. Equality stays correct via `IS`.
- Reading `.Value` on a `NULL` nullable column returns the type default instead of throwing `InvalidOperationException`.
- A projected entity reads back as `null` when all of its mapped columns are `NULL`, so a row whose values are all null cannot be told apart from a missing outer-join row.

## Aggregates

- A grouped `Min`, `Max` or `Average` over a per-group filter that matches no rows returns the type default instead of throwing. `Sum` returns `0`, the same as LINQ.

## Dates, times and storage

- `AddMonths` and `AddYears` whose result lands in December of year 9999 return the default date, since the date math overflows past SQLite's maximum date.
- `DateTimeOffset` drops its offset.
- Date and time component access (`.Year`, `.Day`, `.Days`, ...) in `Where`/`OrderBy` needs `Integer` or `Ticks` storage.
- A value stored as `Text` compares and orders by the stored string, not by its value. This covers `enum`, `TimeSpan`, `DateOnly`, `TimeOnly`, `DateTime` and `decimal`, and the `HasFlag`, bitwise, comparison and cast operators on a `Text`-stored enum.

## R-Tree

- On the default `Float` storage, coordinates are 32-bit floats, so values above 2^24 lose precision. Use `SQLiteRTreeStorage.Int32` for exact integer coordinates.

## Functions and JSON collections

- `SQLiteFunctions.Min` and `Max` need two or more arguments.
- On a JSON array, `ElementAt` past the end, `First`, `Last` or `Single` over an empty array, `Single` over two or more elements, and `Min`/`Max`/`Average`/`Sum` over an empty array all return the type default instead of throwing.
