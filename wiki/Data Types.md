# Data Types

The following .NET types are supported. Each one maps to a SQLite storage class automatically.

| .NET Type | SQLite Storage | Notes |
|---|---|---|
| `int` | INTEGER | |
| `long` | INTEGER | |
| `short` | INTEGER | |
| `byte` | INTEGER | |
| `sbyte` | INTEGER | |
| `uint` | INTEGER | |
| `ulong` | INTEGER | |
| `ushort` | INTEGER | |
| `bool` | INTEGER | `0` or `1` |
| `float` | REAL | |
| `double` | REAL | |
| `decimal` | REAL | Stored as `double`, see note below |
| `string` | TEXT | |
| `char` | TEXT | |
| `Guid` | TEXT | |
| `DateTime` | INTEGER | Stored as ticks |
| `DateTimeOffset` | INTEGER | Stored as ticks, offset is not preserved |
| `DateOnly` | INTEGER | Stored as ticks |
| `TimeOnly` | INTEGER | Stored as ticks |
| `TimeSpan` | INTEGER | Stored as ticks |
| `enum` | INTEGER | Stored as the underlying integer value |
| `byte[]` | BLOB | |

All of these also work as nullable, for example `int?`, `string?`, `DateTime?`.

## Important Notes

**decimal** is stored as a `double`. This means values with more than 15-16 significant digits may lose precision. If exact decimal arithmetic matters, store the value as a `string` and convert manually.

**DateTime** is stored as the raw tick count, not as a formatted string. Comparisons and ordering work correctly.

**DateTimeOffset** is stored as the tick count only. When you read a value back, the offset is always zero. If you need the original offset, store it in a separate column.

**Guid** is stored as a lowercase hyphenated string, for example `3f2504e0-4f89-11d3-9a0c-0305e82c3301`.

**enum** values are stored as their underlying integer. The mapping is stable as long as you do not reorder the enum members.

## Example

```csharp
public class Event
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Name { get; set; }

    public Guid ExternalId { get; set; }

    public DateTime StartsAt { get; set; }

    public TimeSpan Duration { get; set; }

    public EventStatus Status { get; set; }

    public bool IsPublic { get; set; }

    public decimal TicketPrice { get; set; }

    public byte[]? Poster { get; set; }
}

public enum EventStatus
{
    Draft = 0,
    Published = 1,
    Cancelled = 2
}
```
