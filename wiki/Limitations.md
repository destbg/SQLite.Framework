# Limitations

Where query behavior differs from LINQ-to-Objects. See [Storage Options](Storage%20Options) for the storage modes referenced below.

## Numbers

- Divide or modulo by zero is `NULL` (reads back `0` for a non-nullable result, `null` for a nullable one).
- `Math.Sqrt`, `Math.Log` and `Math.Acos` out of domain are `NULL`. SQLite has no `NaN` or infinity.
- Float `ToString()` keeps the decimal point, so `1.0` becomes `"1.0"`.
- `decimal` is not exact: `Real` storage is a 64-bit float, `Text` storage casts to float for compare and order.
- Integer overflow throws `OverflowException`. A `Sum` past 64 bits throws `SQLiteException`, and `Average` stays finite where .NET would throw.
- `uint` and `ulong` arithmetic wraps while the result fits 64 bits, then throws.
- `.Equals` compares by value, so `intColumn.Equals(5L)` is `true` in SQL but `false` in .NET, where `object.Equals` on two different boxed numeric types is always false.
- `Math.Round` with `AwayFromZero` can differ in the last digit.
- `NaN` does not round-trip (stored as `NULL`). Infinity is fine.
- `Parse` and narrowing casts over a column map to `CAST` and do not validate or throw.

## Strings

- `Length` counts code points, and `PadLeft`/`PadRight` measure the target width the same way.
- Ordering and comparison use byte value (`BINARY`), so `"B"` sorts before `"a"`.
- `Substring`, `Remove`, `Insert`, `IndexOf` and `LastIndexOf` clamp out-of-range arguments instead of throwing.
- `Replace("", ...)` returns the original string.
- `ToUpper` and `ToLower` fold only ASCII unless the SQLite build has ICU.
- Concatenating a non-string column keeps its stored form (`bool` to `1`/`0`, `enum` to its number, `DateTime` to ticks or text), matching EF Core.

## Ordering and set operations

- Chained `OrderBy` keeps only the last key, like EF Core. Use `ThenBy` to keep both.
- `Union`, `Distinct`, `Intersect` and `Except` dedup by value, not by reference.

## Null comparisons

- `>`, `<`, `>=`, `<=` on a `NULL` column are `NULL`: the row drops in `Where`/`All`, reads as `false` in `ToList`, and throws in `First`/`Single`. Equality stays correct via `IS`.

## Dates, times and storage

- `DateTimeOffset` drops its offset.
- Date and time component access (`.Year`, `.Day`, `.Days`, ...) in `Where`/`OrderBy` needs `Integer` or `Ticks` storage.
- A value stored as `Text` compares and orders by the stored string, not by its value. This covers `enum`, `TimeSpan`, `DateOnly`, `TimeOnly`, `DateTime` and `decimal`, and the `HasFlag`, bitwise, comparison and cast operators on a `Text`-stored enum.

## Functions and JSON collections

- `SQLiteFunctions.Min` and `Max` need two or more arguments.
- On a JSON array, `ElementAt` past the end and `Min`/`Max`/`Average`/`Sum` over an empty array return the type default instead of throwing.
