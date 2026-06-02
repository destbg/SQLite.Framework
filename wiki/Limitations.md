# Limitations

This page documents query behavior that is easy to miss.

## Numbers

**Division or modulo by zero produces `NULL`.** `x / 0` and `x % 0` evaluate to `NULL`. In a non-nullable projection this reads back as `0`, and in a nullable one as `null`. Floating-point division by zero, such as `5.0 / 0.0`, is also `NULL`.

**Some floating-point operations produce `NULL`.** `Math.Sqrt` of a negative number, `Math.Log` of zero or a negative number, and `Math.Acos` of a value outside the range from -1 to 1 evaluate to `NULL`, because SQLite cannot store `NaN` or infinity.

**Floating-point `ToString()` keeps a decimal point.** A `double` or `float` `ToString()` maps to `CAST(x AS TEXT)`. A whole number keeps a decimal point, so `1.0` becomes `"1.0"`.

**`decimal` is not exact.** With the default `Real` storage a `decimal` is kept as a 64-bit float and loses precision past about 15 digits. With `Text` storage the full value is stored, but comparisons and ordering cast it to a float and lose precision inside the query. See [Storage Options](Storage%20Options).

**Integer overflow throws.** Arithmetic is computed in 64-bit and then read back into the result type. If the value does not fit, reading it throws `OverflowException`. This happens for an `int * int` product or a `Sum` over an `int` column that goes past `int.MaxValue`. Cast to a wider type, such as `(long)a * b`, to compute in 64-bit.

## Strings

**Length counts code points.** `s.Length` returns the number of Unicode code points. A character outside the Basic Multilingual Plane, such as an emoji, counts as 1.

**Ordering and comparison are by byte value.** `OrderBy`, `string.Compare`, `s.CompareTo`, and the `<` and `>` operators use SQLite's `BINARY` collation, which compares by byte value. Uppercase letters sort before lowercase ones, so `"B"` sorts before `"a"`.

**`Substring` and `Remove` clamp.** Out-of-range arguments are clamped rather than rejected. `"ab".Substring(0, 5)` returns `"ab"` and `"ab".Substring(5)` returns `""`.

## Null comparisons

**Order comparisons on a `NULL` column are `NULL`.** `>`, `<`, `>=`, and `<=` evaluate to `NULL` when one side is a `NULL` column. In a `Where` clause and in `All` the row is excluded or fails the check. Projected with `ToList` the value reads as `false`. Read as a single scalar with `First` or `Single` it throws, because a `NULL` cannot be read into a non-nullable `bool`. Project to `bool?` or use `ToList` to read it. Equality (`==` and `!=`) stays correct, because it uses `IS` and `IS NOT`.

## Dates and times

**`DateTimeOffset` drops its offset.** It is stored as ticks or as formatted text without the offset. Store the offset in a separate column if you need it. See [Storage Options](Storage%20Options).

**Component access needs numeric storage.** Property access like `.Year`, `.Month`, `.Day`, `.Hour`, `.Days`, and `.TotalHours` in `Where` and `OrderBy` works only with `Integer` or `Ticks` storage. Under `TextFormatted` or `Text` storage these work only in `Select`, where the value is read first and the property is computed in memory. See [Storage Options](Storage%20Options).

## SQLite functions

**`SQLiteFunctions.Min` and `SQLiteFunctions.Max` need two or more arguments.** With a single argument SQLite reads `min(x)` and `max(x)` as the aggregate forms, so the query turns into an aggregate and returns one row instead of one per input row. To aggregate a column, use LINQ's `Min` and `Max`. See [SQLite Functions](SQLite%20Functions).

## JSON-stored collections

**`ElementAt` past the end returns the default.** Indexing a JSON-stored collection runs as a SQL subquery, so an out-of-range index returns no row and reads back as the type default, the same as `ElementAtOrDefault`. It does not throw `ArgumentOutOfRangeException` the way an in-memory list does.
