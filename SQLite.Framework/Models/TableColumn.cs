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
        IsNullable = (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                     || nullabilityInfoContext.Create(property).ReadState == NullabilityState.Nullable
                     || property.GetCustomAttribute<RequiredAttribute>() == null;

        ColumnType = type switch
        {
            not null when type == typeof(string) => SQLiteColumnType.Text,
            not null when type == typeof(byte[]) => SQLiteColumnType.Blob,
            not null when type == typeof(bool) => SQLiteColumnType.Integer,
            not null when type == typeof(char) => SQLiteColumnType.Text,
            not null when type == typeof(DateTime) => SQLiteColumnType.Integer,
            not null when type == typeof(DateTimeOffset) => SQLiteColumnType.Integer,
            not null when type == typeof(decimal) => SQLiteColumnType.Real,
            not null when type == typeof(double) => SQLiteColumnType.Real,
            not null when type == typeof(float) => SQLiteColumnType.Real,
            not null when type == typeof(Guid) => SQLiteColumnType.Text,
            not null when type == typeof(int) => SQLiteColumnType.Integer,
            not null when type == typeof(long) => SQLiteColumnType.Integer,
            not null when type == typeof(sbyte) => SQLiteColumnType.Integer,
            not null when type == typeof(short) => SQLiteColumnType.Integer,
            not null when type == typeof(uint) => SQLiteColumnType.Integer,
            not null when type == typeof(ulong) => SQLiteColumnType.Integer,
            not null when type == typeof(ushort) => SQLiteColumnType.Integer,
            not null when type.IsEnum => SQLiteColumnType.Integer,
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
        string nullability = IsNullable ? "NULL" : "NOT NULL";
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