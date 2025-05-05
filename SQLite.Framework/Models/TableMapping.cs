using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
        Columns = properties
            .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
            .Select(p => new TableColumn(p))
            .ToArray();

        PrimaryKey = Columns.FirstOrDefault(c => c.IsPrimaryKey)
                     ?? throw new InvalidOperationException($"The class {type.Name} does not have a primary key defined.");
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
    /// The primary key column of the database table.
    /// </summary>
    public TableColumn PrimaryKey { get; }

    /// <summary>
    /// The columns of the database table.
    /// </summary>
    public TableColumn[] Columns { get; }
}