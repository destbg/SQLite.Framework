# Limitations

Where query behavior differs from LINQ-to-Objects. See [Storage Options](Storage%20Options) for the storage modes referenced below.

## Numbers

- Divide or modulo by zero is `NULL` (reads back `0` for a non-nullable result, `null` for a nullable one).
- A `double`, `float` or `decimal` modulo where the value divided by the divisor lands outside the 64-bit integer range returns a wrong number instead of the true remainder, since the whole-part step casts to a 64-bit integer and clamps. For example `2e19 % 2.0` reads back about `1.55e18` instead of `0`.
- A math call whose .NET result is `NaN` reads back as `null`, for example `Math.Sqrt(-1)`, `Math.Acos(2)` or `Math.Pow(-2, 0.5)`. A result that is infinity, such as `Math.Exp(1000)` or `Math.Atanh(1)`, comes back correct.
- `Math.Log`, `Math.Log2` and `Math.Log10` of zero read back as `null` where .NET returns negative infinity. `Math.Log(a, base)` reads back as `null` when the value is zero or the base is 0, 1 or negative.
- `Math.Cbrt` is computed through `POWER`, so the result can differ from .NET in the last bits. An exact cube such as `Math.Cbrt(64)` can read back just below the exact root.
- Float `ToString()` keeps at most 15 significant digits, prints a value at or above 1e15 in scientific notation, prints negative zero as `"0"`, and prints infinity as `"INF"`.
- `decimal` is not exact: `Real` storage is a 64-bit float, `Text` storage casts to float for compare and order.
- On `Real` decimal storage, `ToString()` formats like a `double`: trailing zeros such as `10.50` are dropped, and a very small or very large value prints in scientific notation. `Text` storage returns the stored .NET string.
- On `Text` decimal storage, `Distinct`, the set operators, and a subquery `Contains` compare the stored text. Two equal values with a different scale, such as `10.0` and `10.00`, are then treated as different.
- `Math.Min`, `Math.Max`, `Math.Clamp`, `Math.Abs`, `Math.Floor`, `Math.Ceiling`, `Math.Truncate` and `Math.Round` over a `Text`-stored `decimal` go through a 64-bit float, so a value with more precision than a `double` can hold reads back rounded.
- `float` math runs in 64-bit precision, so a `float` result can differ from .NET in the last digits, and `ToString()` on a fractional `float` prints the digits of the stored 64-bit value. SQLite has no 32-bit float type.
- Integer overflow throws `OverflowException`. A `Sum` past 64 bits throws `SQLiteException`, and `Average` stays finite where .NET would throw.
- `uint` and `ulong` arithmetic wraps while the result fits 64 bits, then throws.
- A `Sum` over a `ulong` column, including a window `Sum`, throws `SQLiteException` once the running total passes 2^63, even when the true unsigned total still fits a `ulong`. SQLite adds with signed 64-bit integers, so it overflows at half the `ulong` range.
- A `uint` multiplication keeps the full 64-bit product instead of the 32-bit wrapped value, both when widened (`(long)(a * b)`) and when used directly (`a * b == 0u`).
- A widening cast such as `(long)` or `(double)` of an `int` (or a `short`, `ushort`, `sbyte` or `byte`) multiplication or addition that overflows `int` keeps the full 64-bit result instead of the 32-bit wrapped value that .NET produces, and does not throw. For example `(long)(a * a)` where `a` is `100000` reads back `10000000000` instead of `1410065408`. The same product read back as an `int`, such as `a * a`, still throws `OverflowException`. This follows the `uint` rule above.
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
- Reading an `int` column whose stored value is outside the `int` range, which can happen when a value was written through raw SQL, may read back its low 32 bits instead of throwing `OverflowException`, since reading an `int` takes a fast path that does not range-check. The smaller integer types (`short`, `ushort`, `byte`, `sbyte`) always throw when the stored value is out of their range.
- The bitwise complement `~` of a native integer (`nint` or `nuint`) is not supported.

## Strings

- `Length` counts code points, and `PadLeft`/`PadRight` measure the target width the same way.
- Ordering and comparison use byte value (`BINARY`), so `"B"` sorts before `"a"`.
- `Substring`, `Remove`, `Insert`, `IndexOf` and `LastIndexOf` clamp out-of-range arguments instead of throwing.
- Reading a character by index, `s[i]`, with an out-of-range index does not throw the index-out-of-range error that .NET throws. A negative index reads a character counted from the end of the string, and an index at or past the end fails with a different error.
- `Replace("", ...)` returns the original string.
- `ToUpper` and `ToLower`, on both `string` and `char`, fold only ASCII unless the SQLite build has ICU.
- The `CultureInfo` overloads of `ToUpper` and `ToLower`, on both `string` and `char` throw in a `Where`.
- Case-insensitive `Equals`, `Compare`, `Contains`, `StartsWith` and `EndsWith` (`OrdinalIgnoreCase`) also fold only ASCII.
- `string.Compare` and `CompareTo` order by byte value, the same as the comparison operators, even when a `CultureInfo` or a culture-aware `StringComparison` such as `InvariantCulture` is given. The sign of the result can differ from .NET, which compares by language rules.
- `Enum.Parse` of a string that is not a defined member name and not a number reads back as the enum's zero value instead of throwing.
- `Enum.Parse` of a numeric string that does not fit the enum's underlying type, such as `300` for a `byte` backed enum, wraps to a value in range instead of throwing `OverflowException`.
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
- The `DefaultIfEmpty` overload that takes an explicit default value is not supported on a table query and throws. The no-argument `DefaultIfEmpty()` used to build a left join works.
- `Contains` over an inline collection literal, such as `new[] { ... }.Contains(column)` or `new List<T> { ... }.Contains(column)`, works only when every element is a constant or a captured value. An element that is a method call, such as `int.Parse("10")`, is not folded to a value, so the query throws `NotSupportedException`. Assign the collection to a variable first, then call `Contains` on the variable.

## Grouping

- Inside a `GroupBy` projection, a `Select` on the group followed by `Distinct`, for example `g.Select(x => x.Name).Distinct().Count()` to count the distinct values in a group, is not supported and throws.

## Joins and SelectMany

- A correlated subquery used directly as a second `from` source, for example `from a in db.Table<Author>() from b in db.Table<Book>().Where(b => b.AuthorId == a.Id)`, is not supported, since SQLite has no `LATERAL` join.
- In a common table expression, a positional constructor projection whose parameter names or order do not match the entity's properties lines the columns up wrong or cannot be read back.

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
- With `DateTimeOffset` stored as `Ticks` (the default), a comparison, ordering, `Distinct` or subtraction across rows whose offsets differ uses the stored local clock ticks, not the UTC instant, so the result can differ from .NET, which normalizes to UTC first. For example `a < b` with `a` at `12:00 +02:00` and `b` at `08:00 -03:00` reads back `false` where .NET gives `true`, and `a - b` reads back `04:00` where .NET gives `-01:00`. Store as `UtcTicks` to compare and order by the instant.
- With `DateTimeOffset` stored as `UtcTicks`, a date or time component read in a query (`.Year`, `.Hour`, ...) comes back in UTC, not in the value's own offset.
- Adding a `TimeSpan` column to a `DateTime` does not work when the `TimeSpan` is stored as `Text`, because the stored text cannot be added as a duration. A constant or captured `TimeSpan` works.
- A `DateTime` stored as `Integer` or `Text` ticks reads back with `Kind` set to `Unspecified`, since the tick count carries no kind.
- Date and time component access (`.Year`, `.Day`, `.Days`, ...) in `Where`/`OrderBy` needs `Integer` or `Ticks` storage.
- A value stored as `Text` compares and orders by the stored string, not by its value. This covers `enum`, `TimeSpan`, `DateOnly`, `TimeOnly`, `DateTime` and `decimal`, and the `HasFlag`, bitwise, comparison and cast operators on a `Text`-stored enum.

## R-Tree

- On the default `Float` storage, coordinates are stored as 32-bit floats. A value above 2^24, or a fractional value that a 32-bit float cannot hold exactly such as `0.2`, loses precision and can miss an exact boundary match.

## Functions and JSON collections

- `SQLiteFunctions.Min` and `Max` need two or more arguments.
- On a JSON array, `ElementAt` past the end, `First`, `Last` or `Single` over an empty array, `Single` over two or more elements, and `Min`/`Max`/`Average`/`Sum` over an empty array all return the type default instead of throwing.
- On a JSON array, `First`, `Last` or `Single` with a predicate that matches no element, and `Min`, `Max` or `Average` after a `Where` that removes every element, also return the type default instead of throwing, the same as their over-empty forms.
- On a JSON array that holds a `null` element, `Except` and `Intersect` against another list that also holds `null` drop the rows that SQL `NOT IN` and `IN` cannot decide through `NULL`, and `Distinct().Count()` leaves the `null` out of the count.
- A JSON list of `double` cannot store `NaN`, `+Infinity` or `-Infinity`. JSON has no way to write these values, so adding a list that holds one fails.
- `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly` and `TimeSpan` values inside a JSON list are kept as text. Reading a part like `.Year`, or comparing them, follows the same rules as `Text` storage, not .NET, so results can differ.
- `Skip` and `Take` on a JSON list take a fixed number or a value from a local variable, not a column of the outer row.
- `GetRange` on a JSON list does not check its arguments. Asking for more items than are there returns the items that fit. A negative count returns the whole list, and a negative start index is read as zero. .NET throws in all three cases.
- `ElementAtOrDefault` on a JSON list with an index taken from a column reads the type default when the index is past the end, but a negative column index fails with an error instead of reading the type default.
- Projecting a JSON dictionary's `Keys` or `Values` collection on its own is not supported.
- Building a new collection from a JSON list with `ToArray` or `ToHashSet` is not supported.
- `OrderBy` followed by `Reverse` on a JSON list keeps rows that share the same sort key in their first-seen order, not the reversed order that LINQ-to-Objects gives, because the reverse is done by sorting the other way.
- On a JSON dictionary, `ContainsKey` and the indexer work in a `Where` or `OrderBy` only with a constant key. A key taken from a column or variable, and `Dictionary.Contains` of a whole key-value pair, are not supported there.
- On a JSON dictionary, the indexer for a key that is not present returns the type default instead of throwing.
- On a JSON list of enums, `Contains` and comparisons use the enum form from the global enum storage mode. When the enum is written inside the JSON in a different form, by default as a number, the comparison does not match.
- A `[JsonPropertyName]` whose name contains a character that the JSON writer escapes, such as an apostrophe, reads back its value only on newer SQLite builds. The writer stores the escaped form (for example `it's`), and an older build, such as the one bundled with SQLCipher, does not match it during a query and returns the type default. A name with an unescaped special character, such as a dot, works on all builds.

## Binary data

- A `byte[]` column supports `Length` and value equality (`==` and `SequenceEqual`) in a query. Reading a single byte by index, and `Contains` of a single byte, are not supported in a query.

## Custom converters

- A `bool` column whose converter stores a non-numeric value, such as the text `yes`/`no`, does not work when used directly as a condition, for example `Where(r => r.Flag)` or `r.Flag && other`. SQLite reads the stored text as the number `0`, so the condition is always false.

## Full text search

- On an external-content FTS5 table, reading an indexed column value works only when the content table's column has the same name as the indexed property. A column renamed with `[Column]` can still be matched, but its value cannot be read back.

## Projections

- A projection that builds an object (`Select(r => new Dto { ... })`) binds public properties only. Public fields are left at their default value.
- Chaining a second `Select` that reads a member set through the constructor of an object built with both constructor arguments and an object initializer, such as `Select(r => new Dto(a) { Note = b }).Select(d => d.A)`, is not supported and throws.
- Calling `GetType` on a value that is `null` throws a different error than LINQ-to-Objects.

## Schema

- An attribute foreign key (`[ReferencesTable]` or `[ForeignKey]`) reads the name of the column it points at on the target table from the target type, before the model builder runs. Renaming that target column with the fluent `HasColumnName` afterward does not reach the foreign key, so it keeps the old name and the table fails to accept rows.
- A composite primary key cannot have an auto-increment member. SQLite only allows auto-increment on a single-column `INTEGER PRIMARY KEY`, so creating such a table throws.
- Auto-increment is only allowed on a single-column `INTEGER PRIMARY KEY`. Marking a key of another type, such as a `string` key, as auto-increment throws when the table is created.
- Migrating a column from nullable to NOT NULL fails when existing rows hold `NULL` and the column has no default. Add a default, set a value with `TableChanged(s => s.Set(...))`, or keep the column nullable. When the column has a default, the existing `NULL` rows are filled with that default.

## Writes

- An `Upsert` that inserts a row writes the new auto-increment key back to the object only when the new row id differs from the last inserted row id on the connection. An earlier insert, even into another table, that already left the same id stops the write-back.
- An `Upsert` with a `DoUpdate` action always writes the object's value for every column, even one left at its CLR default that has a database `DEFAULT`. This is needed so a conflict updates the row to the incoming value through `excluded`. So a fresh insert through `DoUpdate` stores the CLR default rather than the database default, unlike `Add`, `AddOrUpdate` or an `Upsert` with `DoNothing`.

## Raw SQL

- Two `FromSql` fragments composed in the same query that use the same parameter name share one bound value, so the last value wins.
