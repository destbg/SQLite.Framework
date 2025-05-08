using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Models;

/// <summary>
/// Represents a column in a database table.
/// </summary>
public class TableColumn
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableColumn"/> class.
    /// </summary>
    public TableColumn(PropertyInfo property)
    {
        ColumnAttribute? columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
        Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        KeyAttribute? keyProperty = property.GetCustomAttribute<KeyAttribute>();
        NullabilityInfoContext nullabilityInfoContext = new();

        PropertyInfo = property;
        Name = columnAttribute?.Name ?? property.Name;
        PropertyType = type;
        IsPrimaryKey = keyProperty != null;
        IsAutoIncrement = property.GetCustomAttribute<AutoIncrementAttribute>() != null;
        IsNullable = !IsPrimaryKey && (
            Nullable.GetUnderlyingType(property.PropertyType) != null
                || nullabilityInfoContext.Create(property).ReadState == NullabilityState.Nullable
        );

        if (IsNullable && property.GetCustomAttribute<RequiredAttribute>() != null)
        {
            IsNullable = false;
        }

        ColumnType = type switch
        {
            _ when type == typeof(string) => SQLiteColumnType.Text,
            _ when type == typeof(byte[]) => SQLiteColumnType.Blob,
            _ when type == typeof(bool) => SQLiteColumnType.Integer,
            _ when type == typeof(char) => SQLiteColumnType.Text,
            _ when type == typeof(DateTime) => SQLiteColumnType.Integer,
            _ when type == typeof(DateTimeOffset) => SQLiteColumnType.Integer,
            _ when type == typeof(DateOnly) => SQLiteColumnType.Integer,
            _ when type == typeof(TimeOnly) => SQLiteColumnType.Integer,
            _ when type == typeof(Guid) => SQLiteColumnType.Text,
            _ when type == typeof(TimeSpan) => SQLiteColumnType.Integer,
            _ when type == typeof(decimal) => SQLiteColumnType.Real,
            _ when type == typeof(double) => SQLiteColumnType.Real,
            _ when type == typeof(float) => SQLiteColumnType.Real,
            _ when type == typeof(byte) => SQLiteColumnType.Integer,
            _ when type == typeof(int) => SQLiteColumnType.Integer,
            _ when type == typeof(long) => SQLiteColumnType.Integer,
            _ when type == typeof(sbyte) => SQLiteColumnType.Integer,
            _ when type == typeof(short) => SQLiteColumnType.Integer,
            _ when type == typeof(uint) => SQLiteColumnType.Integer,
            _ when type == typeof(ulong) => SQLiteColumnType.Integer,
            _ when type == typeof(ushort) => SQLiteColumnType.Integer,
            _ when type.IsEnum => SQLiteColumnType.Integer,
            _ => throw new NotSupportedException($"The type {type} is not supported.")
        };
    }

    /// <summary>
    /// The name of the column in the database.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The property info of the column in the class.
    /// </summary>
    public PropertyInfo PropertyInfo { get; }

    /// <summary>
    /// The type of the property in the class.
    /// </summary>
    public Type PropertyType { get; }

    /// <summary>
    /// The type of the column in the database.
    /// </summary>
    public SQLiteColumnType ColumnType { get; }

    /// <summary>
    /// Indicates whether the column is a primary key.
    /// </summary>
    public bool IsPrimaryKey { get; }

    /// <summary>
    /// Indicates whether the column is an auto-incrementing primary key.
    /// </summary>
    public bool IsAutoIncrement { get; }

    /// <summary>
    /// Indicates whether the column can be null.
    /// </summary>
    public bool IsNullable { get; }

    /// <summary>
    /// Gets the SQL statement to create the column in the database.
    /// </summary>
    public string GetCreateColumnSql()
    {
        string columnType = ColumnType.ToString().ToUpperInvariant();
        string nullability = IsPrimaryKey ? string.Empty : IsNullable ? "NULL" : "NOT NULL";
        string primaryKey = IsPrimaryKey ? "PRIMARY KEY" : string.Empty;
        string autoIncrement = IsAutoIncrement ? "AUTOINCREMENT" : string.Empty;

        return string.Join(' ', new[]
        {
            Name,
            columnType,
            nullability,
            primaryKey,
            autoIncrement
        }.Where(s => !string.IsNullOrEmpty(s)));
    }
}