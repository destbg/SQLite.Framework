using SQLite.Framework.Enums;

namespace SQLite.Framework;

/// <summary>
/// Defines how a custom .NET type is converted to and from a SQLite column value.
/// </summary>
public interface ISQLiteTypeConverter
{
    /// <summary>
    /// The .NET type this converter handles.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// The SQLite column type used to store values of <see cref="Type" />.
    /// </summary>
    SQLiteColumnType ColumnType { get; }

    /// <summary>
    /// Converts a .NET value to the primitive value that will be stored in SQLite.
    /// </summary>
    object? ToDatabase(object? value);

    /// <summary>
    /// Converts a primitive value read from SQLite back to the .NET type.
    /// </summary>
    object? FromDatabase(object? value);

    /// <summary>
    /// Optional SQL format string that wraps the parameter placeholder when writing.
    /// <c>{0}</c> is replaced by the placeholder (e.g. <c>@p0</c>).
    /// Example: <c>"jsonb({0})"</c> produces <c>jsonb(@p0)</c> in INSERT and UPDATE statements.
    /// </summary>
    string? ParameterSqlExpression => null;

    /// <summary>
    /// Optional SQL format string that wraps the column reference when reading.
    /// <c>{0}</c> is replaced by the column expression (e.g. <c>t0.col</c>).
    /// Example: <c>"json({0})"</c> produces <c>json(t0.col) AS "col"</c> in SELECT statements.
    /// </summary>
    string? ColumnSqlExpression => null;
}
