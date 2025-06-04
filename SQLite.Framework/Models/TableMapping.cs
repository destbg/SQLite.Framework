using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Models;

/// <summary>
/// Represents a mapping between a class and a database table.
/// </summary>
public class TableMapping
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableMapping"/> class.
    /// </summary>
    public TableMapping([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        PropertyInfo[] properties = type.GetProperties();
        TableAttribute? tableAttribute = type.GetCustomAttribute<TableAttribute>();

        Type = type;
        TableName = tableAttribute?.Name ?? type.Name;
        WithoutRowId = type.GetCustomAttribute<WithoutRowIdAttribute>() != null;
        Columns = properties
            .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
            .Select(p => new TableColumn(p))
            .ToArray();
    }

    /// <summary>
    /// The type of the class that maps to the database table.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// The name of the database table.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// The columns of the database table.
    /// </summary>
    public IReadOnlyList<TableColumn> Columns { get; }

    /// <summary>
    /// Indicates that a table does not have a RowId within the table.
    /// </summary>
    public bool WithoutRowId { get; }
}