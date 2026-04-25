using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using SQLite.Framework.Attributes;
using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Models;

/// <summary>
/// Represents a mapping between a class and a database table.
/// </summary>
public class TableMapping
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableMapping"/> class.
    /// </summary>
    public TableMapping([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, SQLiteOptions options)
    {
        PropertyInfo[] properties = type.GetProperties();
        TableAttribute? tableAttribute = type.GetCustomAttribute<TableAttribute>();

        Type = type;
        TableName = tableAttribute?.Name ?? type.Name;
        WithoutRowId = type.GetCustomAttribute<WithoutRowIdAttribute>() != null;
        FullTextSearch = FtsMappingReader.TryRead(type);
        Columns = properties
            .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
            .Where(p => FullTextSearch == null || IsFtsColumn(p))
            .Select(p => new TableColumn(p, options, IsFtsRowIdProperty(p)))
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

    /// <summary>
    /// FTS5 metadata for this table when the class is decorated with
    /// <see cref="FullTextSearchAttribute" />, otherwise <see langword="null" />.
    /// </summary>
    public FtsTableInfo? FullTextSearch { get; }

    /// <summary>
    /// Convenience: <see langword="true" /> when this mapping describes an FTS5 virtual table.
    /// </summary>
    public bool IsFullTextSearch => FullTextSearch != null;

    private bool IsFtsRowIdProperty(PropertyInfo property)
    {
        if (FullTextSearch?.RowId == null)
        {
            return false;
        }

        return FullTextSearch.RowId.Property == property;
    }

    private bool IsFtsColumn(PropertyInfo property)
    {
        if (FullTextSearch == null)
        {
            return true;
        }

        if (IsFtsRowIdProperty(property))
        {
            return true;
        }

        return FullTextSearch.IndexedColumns.Any(c => c.Property == property);
    }
}