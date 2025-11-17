using System.Linq.Expressions;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;

namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Handles the conversion of common object properties to their respective SQL expressions.
/// </summary>
internal class PropertyVisitor
{
    private readonly SQLVisitor visitor;

    public PropertyVisitor(SQLVisitor visitor)
    {
        this.visitor = visitor;
    }

    public Expression HandleNullableProperty(string propertyName, Type type, SQLExpression node)
    {
        return propertyName switch
        {
            nameof(Nullable<>.HasValue) => new SQLExpression(
                type,
                visitor.IdentifierIndex++,
                $"({node.Sql} IS NOT NULL)",
                node.Parameters
            ),
            _ => node
        };
    }

    public Expression HandleStringProperty(string propertyName, Type type, SQLExpression node)
    {
        return propertyName switch
        {
            nameof(string.Length) => new SQLExpression(
                type,
                visitor.IdentifierIndex++,
                $"LENGTH({node.Sql})",
                node.Parameters
            ),
            _ => node
        };
    }

    public Expression HandleDateTimeProperty(string propertyName, Type type, SQLExpression node)
    {
        return propertyName switch
        {
            nameof(DateTime.Year) => ResolveDateFormat(type, node, "Y", "DATETIME"),
            nameof(DateTime.Month) => ResolveDateFormat(type, node, "m", "DATETIME"),
            nameof(DateTime.Day) => ResolveDateFormat(type, node, "d", "DATETIME"),
            nameof(DateTime.Hour) => ResolveDateFormat(type, node, "H", "DATETIME"),
            nameof(DateTime.Minute) => ResolveDateFormat(type, node, "M", "DATETIME"),
            nameof(DateTime.Second) => ResolveDateFormat(type, node, "S", "DATETIME"),
            nameof(DateTime.Millisecond) => new SQLExpression(
                type,
                visitor.IdentifierIndex++,
                $"({node.Sql} / {TimeSpan.TicksPerMillisecond}) % 1000",
                node.Parameters
            ),
            nameof(DateTime.Ticks) => node,
            nameof(DateTime.DayOfWeek) => ResolveDateFormat(type, node, "w", "DATETIME"),
            nameof(DateTime.DayOfYear) => ResolveDateFormat(type, node, "j", "DATETIME"),
            _ => node
        };
    }

    public Expression HandleDateTimeOffsetProperty(string propertyName, Type type, SQLExpression node)
    {
        return propertyName switch
        {
            nameof(DateTimeOffset.Year) => ResolveDateFormat(type, node, "Y", "DATETIME"),
            nameof(DateTimeOffset.Month) => ResolveDateFormat(type, node, "m", "DATETIME"),
            nameof(DateTimeOffset.Day) => ResolveDateFormat(type, node, "d", "DATETIME"),
            nameof(DateTimeOffset.Hour) => ResolveDateFormat(type, node, "H", "DATETIME"),
            nameof(DateTimeOffset.Minute) => ResolveDateFormat(type, node, "M", "DATETIME"),
            nameof(DateTimeOffset.Second) => ResolveDateFormat(type, node, "S", "DATETIME"),
            nameof(DateTimeOffset.Millisecond) => new SQLExpression(
                type,
                visitor.IdentifierIndex++,
                $"({node.Sql} / {TimeSpan.TicksPerMillisecond}) % 1000",
                node.Parameters
            ),
            nameof(DateTimeOffset.Ticks) => node,
            nameof(DateTimeOffset.DayOfWeek) => ResolveDateFormat(type, node, "w", "DATETIME"),
            nameof(DateTimeOffset.DayOfYear) => ResolveDateFormat(type, node, "j", "DATETIME"),
            _ => node
        };
    }

    public Expression HandleTimeSpanProperty(string propertyName, Type type, SQLExpression node)
    {
        return propertyName switch
        {
            nameof(TimeSpan.Days) => new SQLExpression(
                type,
                visitor.IdentifierIndex++,
                $"CAST({node.Sql} / {TimeSpan.TicksPerDay} AS INTEGER)",
                node.Parameters
            ),
            nameof(TimeSpan.TotalDays) => new SQLExpression(
                type,
                visitor.IdentifierIndex++,
                $"(CAST({node.Sql} AS REAL) / {TimeSpan.TicksPerDay})",
                node.Parameters
            ),
            nameof(TimeSpan.Hours) => new SQLExpression(
                type,
                visitor.IdentifierIndex++,
                $"({node.Sql} / {TimeSpan.TicksPerHour}) % 24",
                node.Parameters
            ),
            nameof(TimeSpan.TotalHours) => new SQLExpression(
                type,
                visitor.IdentifierIndex++,
                $"(CAST({node.Sql} AS REAL) / {TimeSpan.TicksPerHour})",
                node.Parameters
            ),
            nameof(TimeSpan.Minutes) => new SQLExpression(
                type,
                visitor.IdentifierIndex++,
                $"({node.Sql} / {TimeSpan.TicksPerMinute}) % 60",
                node.Parameters
            ),
            nameof(TimeSpan.TotalMinutes) => new SQLExpression(
                type,
                visitor.IdentifierIndex++,
                $"(CAST({node.Sql} AS REAL) / {TimeSpan.TicksPerMinute})",
                node.Parameters
            ),
            nameof(TimeSpan.Seconds) => new SQLExpression(
                type,
                visitor.IdentifierIndex++,
                $"({node.Sql} / {TimeSpan.TicksPerSecond}) % 60",
                node.Parameters
            ),
            nameof(TimeSpan.TotalSeconds) => new SQLExpression(
                type,
                visitor.IdentifierIndex++,
                $"(CAST({node.Sql} AS REAL) / {TimeSpan.TicksPerSecond})",
                node.Parameters
            ),
            nameof(TimeSpan.Milliseconds) => new SQLExpression(
                type,
                visitor.IdentifierIndex++,
                $"({node.Sql} / {TimeSpan.TicksPerMillisecond}) % 1000",
                node.Parameters
            ),
            nameof(TimeSpan.TotalMilliseconds) => new SQLExpression(
                type,
                visitor.IdentifierIndex++,
                $"(CAST({node.Sql} AS REAL) / {TimeSpan.TicksPerMillisecond})",
                node.Parameters
            ),
            _ => node
        };
    }

    public Expression HandleDateOnlyProperty(string propertyName, Type type, SQLExpression node)
    {
        return propertyName switch
        {
            nameof(DateOnly.Year) => ResolveDateFormat(type, node, "Y", "DATE"),
            nameof(DateOnly.Month) => ResolveDateFormat(type, node, "m", "DATE"),
            nameof(DateOnly.Day) => ResolveDateFormat(type, node, "d", "DATE"),
            nameof(DateTime.DayOfWeek) => ResolveDateFormat(type, node, "w", "DATE"),
            nameof(DateTime.DayOfYear) => ResolveDateFormat(type, node, "j", "DATE"),
            _ => node
        };
    }

    public Expression HandleTimeOnlyProperty(string propertyName, Type type, SQLExpression node)
    {
        return propertyName switch
        {
            nameof(TimeOnly.Hour) => ResolveTimeFormat(type, node, "H"),
            nameof(TimeOnly.Minute) => ResolveTimeFormat(type, node, "M"),
            nameof(TimeOnly.Second) => ResolveTimeFormat(type, node, "S"),
            _ => node
        };
    }

    private SQLExpression ResolveDateFormat(Type type, SQLExpression obj, string format, string function)
    {
        (SQLiteParameter tickParameter, SQLiteParameter tickToSecondParameter) = CreateHelperDateParameters();

        return new SQLExpression(
            type,
            visitor.IdentifierIndex++,
            $"CAST(STRFTIME('%{format}',{function}(({obj.Sql} - {tickParameter.Name}) / {tickToSecondParameter.Name}, 'unixepoch')) AS INTEGER)",
            [.. obj.Parameters ?? [], tickParameter, tickToSecondParameter]
        );
    }

    private SQLExpression ResolveTimeFormat(Type type, SQLExpression obj, string format)
    {
        SQLiteParameter tickToSecondParameter = new()
        {
            Name = $"@p{visitor.ParamIndex.Index++}",
            Value = TimeSpan.TicksPerSecond
        };
        return new SQLExpression(
            type,
            visitor.IdentifierIndex++,
            $"CAST(STRFTIME('%{format}',TIME({obj.Sql} / {tickToSecondParameter.Name}, 'unixepoch')) AS INTEGER)",
            [.. obj.Parameters ?? [], tickToSecondParameter]
        );
    }

    private (SQLiteParameter TickParameter, SQLiteParameter TickToSecondParameter) CreateHelperDateParameters()
    {
        SQLiteParameter tickParameter = new()
        {
            Name = $"@p{visitor.ParamIndex.Index++}",
            Value = Constants.TicksToEpoch
        };
        SQLiteParameter tickToSecondParameter = new()
        {
            Name = $"@p{visitor.ParamIndex.Index++}",
            Value = TimeSpan.TicksPerSecond
        };

        return (tickParameter, tickToSecondParameter);
    }
}