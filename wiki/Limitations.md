# Limitations

Where query behavior differs from LINQ-to-Objects.

## Numbers

Divide or modulo by zero is `NULL` instead of an exception. A non-nullable result reads back `0`, a nullable one reads back `null`.

```csharp
var ratios = await db.Table<Reading>().Select(r => r.Value / r.Divisor).ToListAsync();
// .NET throws DivideByZeroException when Divisor is 0. SQLite returns NULL for that row.
```

Integer overflow throws, since SQLite computes in signed 64-bit integers. A `Sum` past 2^63 throws a `SQLiteException` where .NET would throw `OverflowException`.

`decimal` is not exact. `Real` storage is a 64-bit float and `Text` storage casts to float for compare and order, so a value with more precision than a `double` can hold reads back rounded.

```csharp
var big = await db.Table<Price>().Select(p => p.Amount).FirstAsync();
// Stored 0.1m reads back 0.1000000000000000055... on Real storage, not exactly 0.1m.
```

`float` math runs in 64-bit precision, so a `float` result can differ from .NET in the last digits. SQLite has no 32-bit float type.

`NaN` does not round-trip. It is stored as `NULL`. A math call whose .NET result is `NaN`, such as `Math.Sqrt(-1)`, reads back as `null`. Infinity is fine.

## Strings

Ordering and comparison use byte value, so `"B"` sorts before `"a"` where .NET's culture-aware comparer would not. Case-insensitive comparisons (`OrdinalIgnoreCase`) fold only ASCII.

```csharp
var ordered = await db.Table<Tag>().OrderBy(t => t.Name).Select(t => t.Name).ToListAsync();
// "Banana" comes before "apple", byte by byte. .NET's default string comparer disagrees.
```

`ToUpper` and `ToLower`, on both `string` and `char`, fold only ASCII unless the SQLite build has ICU. `"é".ToUpper()` stays `"é"` in a query but becomes `"É"` in .NET.

A string method over a value that holds an embedded NUL character (`\0`) sees only the text before the NUL. The value itself stores, reads back and compares with `==` as the whole string.

```csharp
var hits = await db.Table<Doc>().Where(d => d.Body.Contains("after")).ToListAsync();
// A Body of "before\0after" does not match. SQLite's text functions stop at the first NUL.
```

`Substring`, `Remove`, `Insert`, `IndexOf` and `LastIndexOf` clamp out-of-range arguments instead of throwing, following SQLite's `SUBSTR`.

## Ordering and grouping

Without an explicit `OrderBy`, row order follows SQLite's query plan rather than insertion order. An index over the read column makes `First` or `Take` read the lowest indexed rows instead of the first inserted ones.

Chained `OrderBy` keeps only the last key, so a second `OrderBy` drops the first key.

```csharp
var rows = await db.Table<Sale>().OrderBy(s => s.Region).OrderBy(s => s.Total).ToListAsync();
// Sorted by Total only. The Region key is gone.
```

`Union`, `Intersect` and `Except` return rows in sorted order and dedup by value, not by reference or first appearance. `Concat` keeps first-appearance order.

`GroupBy` returns groups in key order, not the first-seen order that LINQ-to-Objects uses.

## Query operators

Some LINQ operators are not translated to SQL and throw `NotSupportedException` on a table query. These are `Last`, `LastOrDefault`, `Order`, `OrderDescending`, `MaxBy`, `MinBy`, `DistinctBy`, `SkipLast`, `TakeLast`, `Append`, `Prepend`, `Chunk`, `ExceptBy`, `UnionBy`, `IntersectBy`, `SkipWhile` and `TakeWhile`.

After a `Select` that runs in memory, only `Distinct`, `Take`, `Skip`, `Reverse`, `ElementAt`, `First`, `Single`, `Count` and `Any` without a predicate continue the query. Any other operator throws, because SQLite cannot compute the projected value inside the database.

```csharp
var names = await db.Table<Person>().Select(p => Format(p.Name)).ToListAsync();
// Fine, Format runs in memory per row. Adding .Where(n => n.Length > 3) after it throws.
```

## Null comparisons

`>`, `<`, `>=`, `<=` on a `NULL` column are `NULL`. The row drops in `Where`/`All`, reads as `false` in `ToList` and throws in `First`/`Single`. Equality stays correct via `IS`.

```csharp
var seniors = await db.Table<Person>().Where(p => p.Age > 65).ToListAsync();
// Rows with Age == NULL are simply absent, where a null-conditional check in .NET
// might have treated them as false in a more visible way.
```

Reading `.Value` on a `NULL` nullable column returns the type default instead of throwing `InvalidOperationException`.

A projected entity reads back as `null` when all of its mapped columns are `NULL`, so a row whose values are all null cannot be told apart from a missing outer-join row.

## Aggregates

A grouped `Min`, `Max` or `Average` over a filter that matches no rows returns the type default instead of throwing. `Sum` returns `0`, the same as LINQ.

```csharp
var oldest = await db.Table<Person>().Where(p => p.City == "Nowhere").MaxAsync(p => p.Age);
// .NET throws InvalidOperationException over an empty sequence. SQLite returns 0.
```

A correlated subquery inside a projection returns the type default where LINQ-to-Objects throws, since a SQL scalar subquery cannot throw. `First`, `Single` or a non-nullable `Min` over an empty subquery read back the type default.

## Dates and times

`DateTimeOffset` drops its offset. With the default `Ticks` storage, a comparison or subtraction across rows whose offsets differ uses the stored local clock ticks, not the UTC instant.

```csharp
var earlier = await db.Table<Event>().Where(e => e.Start < cutoff).ToListAsync();
// 12:00 +02:00 vs 08:00 -03:00 compares by local ticks here, by UTC instant in .NET.
```

A value stored as `Text` compares and orders by the stored string, not by its value. This covers `enum`, `TimeSpan`, `DateOnly`, `TimeOnly`, `DateTime` and `decimal`.

Date and time component access (`.Year`, `.Day`, `.Days`, ...) in `Where`/`OrderBy` needs `Integer` or `Ticks` storage. On `Text` storage it throws.

## JSON

On a JSON array, `First`, `Last` or `Single` over an empty array and `Min`/`Max`/`Average`/`Sum` over an empty array return the type default instead of throwing.

```csharp
var best = await db.Table<Cart>().Select(c => c.Prices.Max()).ToListAsync();
// An empty Prices array gives 0, not the .NET InvalidOperationException.
```

Date and time values inside a JSON list are kept as text, so reading a part like `.Year` or ordering them follows the `Text` storage rules, not .NET.

## Schema and migrations

Changing a storage mode option, such as `DecimalStorage`, `CharStorage` or a date or time storage mode, does not re-encode existing rows. A filter that binds the new form does not match the old rows until a data step rewrites them.

```csharp
// Rows written while EnumStorage was Text stay text after switching to Integer.
// Only an enum column moved between Integer and Text is re-encoded during a rebuild.
```

Migrating a column from nullable to NOT NULL fails when existing rows hold `NULL` and the column has no default. When the column has a default, the existing `NULL` rows are filled with that default.

An attribute foreign key (`[ReferencesTable]` or `[ForeignKey]`) reads the name of the column it points at before the model builder runs, so renaming the target column with the fluent `HasColumnName` afterward does not reach the foreign key and the table fails to accept rows.

A composite primary key cannot have an auto-increment member. Auto-increment is only allowed on a single-column `INTEGER PRIMARY KEY`, so creating such a table throws.

## Writes

`AddOrUpdate` and `AddOrUpdateRange` run `INSERT OR REPLACE`. On a key conflict SQLite deletes the old row and inserts a new one, so with foreign keys on, an `ON DELETE CASCADE` action removes the rows that reference the replaced row. `Update` and an `Upsert` with `DoUpdate` change the row in place and keep the referencing rows.

```csharp
await db.Table<Author>().AddOrUpdateAsync(existing);
// The author's row is deleted and re-inserted. Cascade removes its books.
```

An `Upsert` that inserts a row writes the new auto-increment key back to the object only when the new row id differs from the last inserted row id on the connection. An earlier insert, even into another table, that already left the same id stops the write-back.

## Projections

A projection that builds an object (`Select(r => new Dto { ... })`) binds public properties only. Public fields are left at their default value.

```csharp
var dtos = await db.Table<Row>().Select(r => new Dto { Name = r.Name }).ToListAsync();
// Dto.Count, a public field, stays 0 even when the table has a Count column.
```

## Raw SQL

Two `FromSql` fragments composed in the same query that use the same parameter name share one bound value, so the last value wins.

```csharp
var rows = await db.Table<A>().FromSql("SELECT * FROM A WHERE X > @min", minA)
    .Union(db.Table<B>().FromSql("SELECT * FROM B WHERE Y > @min", minB))
    .ToListAsync();
// Both filters bind minB's value.
```

## Backup

`BackupTo` onto a second connection of the same database file throws `InvalidOperationException`. The copy would hold a read lock on the source file that its own destination write can never pass, so it could never finish.
