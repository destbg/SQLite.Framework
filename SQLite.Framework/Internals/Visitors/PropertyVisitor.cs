using System.Linq.Expressions;
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

    public Expression HandleDateTimeProperty(string propertyName, Type type, SQLExpression node)
    {
        switch (propertyName)
        {
            case nameof(DateTime.Year):
                return AppendDateGet(type, node, "Y");
            case nameof(DateTime.Month):
                return AppendDateGet(type, node, "m");
            case nameof(DateTime.Day):
                return AppendDateGet(type, node, "d");
            case nameof(DateTime.Hour):
                return AppendDateGet(type, node, "H");
            case nameof(DateTime.Minute):
                return AppendDateGet(type, node, "M");
            case nameof(DateTime.Second):
                return AppendDateGet(type, node, "S");
            case nameof(DateTime.Millisecond):
                return new SQLExpression(
                    type,
                    visitor.IdentifierIndex++,
                    $"({node.Sql} / {TimeSpan.TicksPerMillisecond}) % 1000",
                    node.Parameters
                );
            case nameof(DateTime.Ticks):
                return node;
            case nameof(DateTime.DayOfWeek):
                return AppendDateGet(type, node, "w");
            case nameof(DateTime.DayOfYear):
                return AppendDateGet(type, node, "j");
            default:
                return node;
        }
    }

    private SQLExpression AppendDateGet(Type type, SQLExpression obj, string format)
    {
        (SQLiteParameter tickParameter, SQLiteParameter tickToSecondParameter) = CreateHelperDateParameters();

        return new SQLExpression(
            type,
            visitor.IdentifierIndex++,
            $"CAST(STRFTIME('%{format}',DATETIME(({obj.Sql} - {tickParameter.Name}) / {tickToSecondParameter.Name}, 'unixepoch')) AS INTEGER)",
            [..obj.Parameters ?? [], tickParameter, tickToSecondParameter]
        );
    }

    private (SQLiteParameter TickParameter, SQLiteParameter TickToSecondParameter) CreateHelperDateParameters()
    {
        SQLiteParameter tickParameter = new()
        {
            Name = $"@p{visitor.ParamIndex.Index++}",
            Value = 621355968000000000 // new DateTime(1970, 1, 1).Ticks
        };
        SQLiteParameter tickToSecondParameter = new()
        {
            Name = $"@p{visitor.ParamIndex.Index++}",
            Value = TimeSpan.TicksPerSecond
        };

        return (tickParameter, tickToSecondParameter);
    }
}