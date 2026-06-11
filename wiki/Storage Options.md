# Storage Options

`SQLiteOptions` controls how specific .NET types are stored in and read from the database. Build one via `SQLiteOptionsBuilder`, then hand the immutable result to `SQLiteDatabase`:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("mydb.sqlite")
    .UseDateTimeStorage(DateTimeStorageMode.TextFormatted, "yyyy-MM-dd HH:mm:ss")
    .Build();

using SQLiteDatabase db = new(options);
```

`UseDateTimeStorage`, `UseDateTimeOffsetStorage`, `UseTimeSpanStorage`, `UseDateOnlyStorage`, `UseTimeOnlyStorage`, `UseDecimalStorage`, `UseEnumStorage`, and `UseCharStorage` each set the corresponding mode and optionally the format string. Chain them together with `AddTypeConverter`, `AddMethodTranslator`, `AddPropertyTranslator`, `UseWalMode`, `UseOpenFlags`, and `UseEncryptionKey` to configure the whole database in one place.

Once you call `Build()`, the returned `SQLiteOptions` is fully read-only. If you need to change a setting, build a new options instance.

---

## DateTime

| Property | Type | Default |
|---|---|---|
| `DateTimeStorage` | `DateTimeStorageMode` | `Integer` |
| `DateTimeFormat` | `string` | `"yyyy-MM-dd HH:mm:ss"` |

| Mode | SQLite type | Example value |
|---|---|---|
| `Integer` | INTEGER | `638000000000000000` |
| `TextTicks` | TEXT | `"638000000000000000"` |
| `TextFormatted` | TEXT | `"2000-02-03 04:05:06"` |

`TextFormatted` uses `DateTimeFormat` as the format string when reading and writing values.

> LINQ property access (`Year`, `Month`, `Day`, etc.) is not supported with `TextFormatted`. Switch to `Integer` if you need it.

`TextTicks` stores the tick count as a TEXT string. It is provided for compatibility with other SQLite libraries that use this format.

---

## DateTimeOffset

| Property | Type | Default |
|---|---|---|
| `DateTimeOffsetStorage` | `DateTimeOffsetStorageMode` | `Ticks` |
| `DateTimeOffsetFormat` | `string` | `"yyyy-MM-dd HH:mm:ss zzz"` |

| Mode | SQLite type | Example value |
|---|---|---|
| `Ticks` | INTEGER | `638000000000000000` |
| `UtcTicks` | INTEGER | `638000000000000000` |
| `TextFormatted` | TEXT | `"2000-02-03 04:05:06 +05:30"` |

`Ticks` stores the local tick count. The original offset is not stored, so you need a separate column to preserve it.

`UtcTicks` stores the UTC tick count. The original offset is not preserved.

`TextFormatted` uses `DateTimeOffsetFormat` as the format string when reading and writing values.

> The same rule as `DateTime` applies. Property access like `Year` or `Month` in a query needs one of the tick modes, not `TextFormatted`.

---

## DateOnly

| Property | Type | Default |
|---|---|---|
| `DateOnlyStorage` | `DateOnlyStorageMode` | `Integer` |
| `DateOnlyFormat` | `string` | `"yyyy-MM-dd"` |

| Mode | SQLite type | Example value |
|---|---|---|
| `Integer` | INTEGER | `638000000000000000` |
| `Text` | TEXT | `"2000-02-03"` |

`Text` uses `DateOnlyFormat` as the format string when reading and writing values.

> With `Text` storage, `Year`, `Month`, and `Day` cannot be used inside `Where` or `OrderBy`. Use `Integer` to query date parts.

---

## TimeOnly

| Property | Type | Default |
|---|---|---|
| `TimeOnlyStorage` | `TimeOnlyStorageMode` | `Integer` |
| `TimeOnlyFormat` | `string` | `"HH:mm:ss"` |

| Mode | SQLite type | Example value |
|---|---|---|
| `Integer` | INTEGER | `144060000000` |
| `Text` | TEXT | `"04:05:06"` |

`Text` uses `TimeOnlyFormat` as the format string when reading and writing values.

> `Hour`, `Minute`, and `Second` only translate to SQL under `Integer` storage.

---

## TimeSpan

| Property | Type | Default |
|---|---|---|
| `TimeSpanStorage` | `TimeSpanStorageMode` | `Integer` |
| `TimeSpanFormat` | `string` | `"c"` |

| Mode | SQLite type | Example value |
|---|---|---|
| `Integer` | INTEGER | `720060000000` |
| `Text` | TEXT | `"2.03:04:05.0060070"` |

`Text` uses `TimeSpanFormat` as the format string. The default `"c"` is the standard constant (invariant) format.

> Component properties such as `Days` and `TotalHours` do not translate under `Text` storage. Pick `Integer` when queries read them.

---

## decimal

| Property | Type | Default |
|---|---|---|
| `DecimalStorage` | `DecimalStorageMode` | `Real` |
| `DecimalFormat` | `string` | `"G"` |

| Mode | SQLite type | Example value |
|---|---|---|
| `Real` | REAL | `1234.5678` |
| `Text` | TEXT | `"1234.5678"` |

`Real` stores the value as a double precision floating point number, which can lose precision for very large or high-precision decimal values.

`Text` stores the full decimal string to preserve precision. When the column is used in a LINQ query (comparisons, ordering, etc.), it is automatically wrapped with `CAST(... AS REAL)` in the generated SQL (which makes it lose precision).

`DecimalFormat` controls the format string used when storing the value as text. The default `"G"` is the general format.

---

## enum

| Property | Type | Default |
|---|---|---|
| `EnumStorage` | `EnumStorageMode` | `Integer` |

| Mode | SQLite type | Example value |
|---|---|---|
| `Integer` | INTEGER | `1` |
| `Text` | TEXT | `"Active"` |

`Text` stores the name of the enum value. This is more readable but takes more space than `Integer`.

---

## Char

| Property | Type | Default |
|---|---|---|
| `CharStorage` | `CharStorageMode` | `Text` |

| Mode | SQLite type | Example value |
|---|---|---|
| `Text` | TEXT | `"A"` |
| `Integer` | INTEGER | `65` |

`Text` stores the character as a single-character string. A lone UTF-16 surrogate cannot be stored this way and reads back as the replacement character.

`Integer` stores the UTF-16 code unit as a number. It round-trips every char value exactly, including lone surrogates.

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .UseCharStorage(CharStorageMode.Integer)
    .Build();
```

---

## String Comparison

| Property | Type | Default |
|---|---|---|
| `CaseSensitiveStringComparison` | `bool` | `false` |

Controls how `string.Contains`, `string.StartsWith`, and `string.EndsWith` translate to SQL.

| Value | Behavior |
|---|---|
| `false` (default) | Translates to `LIKE`, which is case-insensitive for ASCII. |
| `true` | Translates to `instr` / `substr`, which are case-sensitive. This matches .NET in-memory LINQ and the EF Core SQLite provider. |

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .UseCaseSensitiveStringComparison()
    .Build();
```

The `StringComparison.OrdinalIgnoreCase` overloads stay case-insensitive regardless of this option. Note that case-sensitive `StartsWith` cannot use a `LIKE 'prefix%'` index scan.

---

## Auto-Increment Primary Keys

| Property | Type | Default |
|---|---|---|
| `ExplicitAutoIncrementKeysPreserved` | `bool` | `false` |

Controls how the `Add` family of methods (`Add` and `AddRange`) handles the value already set on an `[AutoIncrement]` primary key.

| Value | Behavior |
|---|---|
| `false` (default) | The value on the entity is always overwritten. SQLite assigns a new id and writes it back to the property. |
| `true` | A non-default value (for example, `Id == 5`) is used directly. The row is inserted at that id, and a uniqueness error is thrown if it is already taken. A type-default value (`Id == 0`) still triggers SQLite to assign one. |

Set this to `true` to match Entity Framework Core's `Add` behavior:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .PreserveExplicitAutoIncrementKeys()
    .Build();
```

This option only changes the `Add` family. `AddOrUpdate` already uses a non-default primary key when you set one, with or without the option. See [CRUD Operations](CRUD%20Operations) for the full breakdown and the [EF Core migration guide](Migrating%20from%20EF%20Core) for context on why you might want this.
