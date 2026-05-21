using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Models;

/// <summary>
/// Represents a mapping between a class and a database table.
/// </summary>
public class TableMapping
{
    private readonly List<ForeignKeyInfo> compositeForeignKeys = [];

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
        Strict = type.GetCustomAttribute<StrictTableAttribute>() != null;
        bool hasFts = type.GetCustomAttribute<FullTextSearchAttribute>() != null;
        bool hasRtree = type.GetCustomAttribute<RTreeIndexAttribute>() != null;
        if (hasFts && hasRtree)
        {
            throw new InvalidOperationException(
                $"Entity '{type.Name}' cannot be both an FTS5 and an R-Tree virtual table. Remove one of the attributes.");
        }
        FullTextSearch = FtsMappingReader.TryRead(type);
        RTree = RTreeMappingReader.TryRead(type);
        Columns = properties
            .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
            .Where(p => FullTextSearch == null || IsFtsColumn(p))
            .Where(p => RTree == null || IsRTreeColumn(p))
            .Select(p => new TableColumn(p, options, IsFtsRowIdProperty(p)))
            .ToArray();

        foreach (TableColumn column in Columns)
        {
            if (column.ReferencesTableAttribute is { } typed)
            {
                column.ForeignKey = ResolveTypedForeignKey(column, typed);
            }
            else if (column.ForeignKeyAttribute is { } ef)
            {
                column.ForeignKey = ResolveDataAnnotationsForeignKey(type, column, ef);
            }
        }
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
    /// Indicates that the table is a SQLite <c>STRICT</c> table. SQLite enforces declared column
    /// types on every insert and update. Requires SQLite 3.37.0 or newer.
    /// </summary>
    public bool Strict { get; internal set; }

    /// <summary>
    /// FTS5 metadata for this table when the class is decorated with
    /// <see cref="FullTextSearchAttribute" />, otherwise <see langword="null" />.
    /// </summary>
    public FtsTableInfo? FullTextSearch { get; }

    /// <summary>
    /// Convenience: <see langword="true" /> when this mapping describes an FTS5 virtual table.
    /// </summary>
    public bool IsFullTextSearch => FullTextSearch != null;

    /// <summary>
    /// R-Tree metadata for this table when the class is decorated with
    /// <see cref="RTreeIndexAttribute" />, otherwise <see langword="null" />.
    /// </summary>
    public RTreeTableInfo? RTree { get; }

    /// <summary>
    /// Convenience: <see langword="true" /> when this mapping describes an R-Tree virtual table.
    /// </summary>
    public bool IsRTree => RTree != null;

    /// <summary>
    /// Composite foreign keys declared on this table via the fluent builder. Single-column
    /// foreign keys live on <see cref="TableColumn.ForeignKey" /> instead.
    /// </summary>
    public IReadOnlyList<ForeignKeyInfo> CompositeForeignKeys => compositeForeignKeys;

    internal void AddCompositeForeignKey(ForeignKeyInfo info)
    {
        compositeForeignKeys.Add(info);
    }

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
        if (IsFtsRowIdProperty(property))
        {
            return true;
        }

        return FullTextSearch!.IndexedColumns.Any(c => c.Property == property);
    }

    private bool IsRTreeColumn(PropertyInfo property)
    {
        if (RTree!.RowIdProperty == property)
        {
            return true;
        }

        if (RTree.Bounds.Any(b => b.Property == property))
        {
            return true;
        }

        return RTree.Auxiliaries.Any(a => a.Property == property);
    }

    private ForeignKeyInfo ResolveTypedForeignKey(TableColumn column, ReferencesTableAttribute attribute)
    {
        (string targetTable, string targetColumn) = ForeignKeyResolver.ResolveSingleTarget(
            sourceTable: TableName,
            sourceColumn: column.Name,
            attribute.TargetType,
            attribute.TargetColumn);

        ForeignKeyResolver.ValidateSetNullCompatibility(
            sourceTable: TableName,
            sourceColumns: [column.Name],
            sourceNullability: [column.IsNullable],
            attribute.OnDelete,
            attribute.OnUpdate);

        return new ForeignKeyInfo(
            columns: [column.Name],
            targetTable: targetTable,
            targetColumns: [targetColumn],
            onDelete: attribute.OnDelete,
            onUpdate: attribute.OnUpdate,
            deferred: attribute.Deferred);
    }

    private ForeignKeyInfo ResolveDataAnnotationsForeignKey(Type owner, TableColumn column, ForeignKeyAttribute attribute)
    {
        Type targetType = ForeignKeyResolver.ResolveTargetTypeByName(owner, column.Name, attribute.Name);
        (string targetTable, string targetColumn) = ForeignKeyResolver.ResolveSingleTarget(
            sourceTable: TableName,
            sourceColumn: column.Name,
            targetType,
            targetColumnName: null);

        return new ForeignKeyInfo(
            columns: [column.Name],
            targetTable: targetTable,
            targetColumns: [targetColumn],
            onDelete: SQLiteForeignKeyAction.NoAction,
            onUpdate: SQLiteForeignKeyAction.NoAction,
            deferred: false);
    }
}
