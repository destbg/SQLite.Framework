using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Models;

/// <summary>
/// Represents a column in a database table.
/// </summary>
public class TableColumn
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableColumn"/> class.
    /// </summary>
    public TableColumn(PropertyInfo property, SQLiteOptions options, bool isFtsRowId = false)
    {
        ColumnAttribute? columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
        Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        KeyAttribute? keyProperty = property.GetCustomAttribute<KeyAttribute>();
        NullabilityInfoContext nullabilityInfoContext = new();

        PropertyInfo = property;
        Name = isFtsRowId ? "rowid" : columnAttribute?.Name ?? property.Name;
        PropertyType = type;
        Indices = property.GetCustomAttributes<IndexedAttribute>().ToArray();
        IsPrimaryKey = keyProperty != null;
        IsAutoIncrement = property.GetCustomAttribute<AutoIncrementAttribute>() != null;
        IsFtsRowId = isFtsRowId;
        IsNullable = !IsPrimaryKey && !isFtsRowId && (
            Nullable.GetUnderlyingType(property.PropertyType) != null
            || nullabilityInfoContext.Create(property).ReadState == NullabilityState.Nullable
        );

        if (IsNullable && property.GetCustomAttribute<RequiredAttribute>() != null)
        {
            IsNullable = false;
        }

        ColumnType = TypeHelpers.TypeToSQLiteType(type, options);
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
    /// The indices associated with the column, if any.
    /// </summary>
    public IReadOnlyList<IndexedAttribute> Indices { get; }

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
    /// Indicates whether the column is the implicit <c>rowid</c> on an FTS5 virtual table.
    /// </summary>
    public bool IsFtsRowId { get; }

    /// <summary>
    /// Indicates whether the column can be null.
    /// </summary>
    public bool IsNullable { get; }

    /// <summary>
    /// Gets the SQL statement to create the column in the database.
    /// </summary>
    /// <param name="emitInlinePrimaryKey">
    /// When false, the inline <c>PRIMARY KEY</c> / <c>AUTOINCREMENT</c> clause is omitted so that
    /// a composite key can be declared as a table-level <c>PRIMARY KEY (col1, col2)</c> constraint.
    /// </param>
    public string GetCreateColumnSql(bool emitInlinePrimaryKey = true)
    {
        string columnType = ColumnType.ToString().ToUpperInvariant();
        bool inlinePk = emitInlinePrimaryKey && IsPrimaryKey;
        string nullability = inlinePk ? string.Empty : IsNullable ? "NULL" : "NOT NULL";
        string primaryKey = inlinePk ? "PRIMARY KEY" : string.Empty;
        string autoIncrement = inlinePk && IsAutoIncrement ? "AUTOINCREMENT" : string.Empty;

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