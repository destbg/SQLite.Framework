# Storage Options

`SQLiteStorageOptions` controls how specific .NET types are stored in and read from the database.
You can configure it when creating a database:

```csharp
var db = new SQLiteDatabase("mydb.sqlite", new SQLiteStorageOptions
{
    DateTimeStorage = DateTimeStorageMode.TextFormatted,
    DateTimeFormat = "yyyy-MM-dd HH:mm:ss"
});
```

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

> LINQ property access (`Year`, `Month`, `Day`, etc.) is not supported with `TextFormatted`. Switch to `Ticks` if you need it.

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

> LINQ property access (`Year`, `Month`, `Day`, etc.) is not supported with `Text`. Switch to `Integer` if you need it.

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

> LINQ property access (`Hour`, `Minute`, `Second`) is not supported with `Text`. Switch to `Integer` if you need it.

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

> LINQ property access (`Days`, `Hours`, `TotalDays`, etc.) is not supported with `Text`. Switch to `Integer` if you need it.

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
