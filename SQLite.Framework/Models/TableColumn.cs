using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Helpers;

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

        ColumnType = CommonHelpers.TypeToSQLiteType(type);
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