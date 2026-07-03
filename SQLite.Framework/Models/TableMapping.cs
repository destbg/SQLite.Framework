using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Models;

/// <summary>
/// Represents a mapping between a class and a database table.
/// </summary>
public class TableMapping
{
    private readonly List<TableColumn> columns = [];
    private readonly List<ForeignKeyInfo> compositeForeignKeys = [];
    private readonly List<ComputedColumnSpec> computedColumns = [];
    private readonly List<CheckConstraintSpec> checks = [];
    private readonly List<IndexSpec> indexes = [];
    private readonly List<TriggerSpec> triggers = [];
    private readonly List<ShadowColumnSpec> shadowColumns = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TableMapping"/> class.
    /// </summary>
    public TableMapping([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, SQLiteOptions options)
    {
        PropertyInfo[] properties = type.GetProperties();
        TableAttribute? tableAttribute = type.GetCustomAttribute<TableAttribute>();

        Type = type;
        TableName = tableAttribute?.Name ?? type.Name;
        IdentifierGuard.EnsureNoQuote(TableName, "Table");
        WithoutRowId = type.GetCustomAttribute<WithoutRowIdAttribute>() != null;
        Strict = type.GetCustomAttribute<StrictTableAttribute>() != null;
        bool hasFts = type.GetCustomAttribute<FullTextSearchAttribute>() != null;
        bool hasRtree = type.GetCustomAttribute<RTreeIndexAttribute>() != null;
        if (hasFts && hasRtree)
        {
            throw new InvalidOperationException(
                $"Entity '{type.Name}' cannot combine [FullTextSearch] and [RTreeIndex]. Pick at most one virtual table kind.");
        }
        FullTextSearch = FtsMappingReader.TryRead(type);
        RTree = RTreeMappingReader.TryRead(type);
        columns.AddRange(properties
            .Where(p => p.GetMethod is { IsStatic: false })
            .Where(p => p.GetIndexParameters().Length == 0)
            .Where(p => p.SetMethod != null || p.GetMethod!.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
            .Where(p => FullTextSearch == null || IsFtsColumn(p))
            .Where(p => RTree == null || IsRTreeColumn(p))
            .GroupBy(p => p.Name)
            .Select(g => g.OrderByDescending(p => TypeHelpers.TypeDepth(p.DeclaringType)).First())
            .Select(p => new TableColumn(p, options, IsFtsRowIdProperty(p))));

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
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public Type Type { get; }

    /// <summary>
    /// The name of the database table.
    /// </summary>
    public string TableName { get; internal set; }

    /// <summary>
    /// The columns of the database table.
    /// </summary>
    public IReadOnlyList<TableColumn> Columns => columns;

    /// <summary>
    /// Indicates that a table does not have a RowId within the table.
    /// </summary>
    public bool WithoutRowId { get; internal set; }

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

    /// <summary>
    /// Computed (generated) columns declared on this table through the model. This is the single
    /// source of truth, so create, migrate and validate all agree.
    /// </summary>
    internal IReadOnlyList<ComputedColumnSpec> ComputedColumns => computedColumns;

    /// <summary>
    /// Table-level CHECK constraints declared on this table through the model.
    /// </summary>
    internal IReadOnlyList<CheckConstraintSpec> Checks => checks;

    /// <summary>
    /// Indexes declared on this table through the model. Column-level <c>[Indexed]</c> indexes are
    /// read from the columns instead.
    /// </summary>
    internal IReadOnlyList<IndexSpec> Indexes => indexes;

    /// <summary>
    /// Triggers declared on this table through the model.
    /// </summary>
    internal IReadOnlyList<TriggerSpec> Triggers => triggers;

    /// <summary>
    /// Columns declared on this table through the model that have no CLR property. The framework
    /// creates and keeps them, but never reads or writes them.
    /// </summary>
    internal IReadOnlyList<ShadowColumnSpec> ShadowColumns => shadowColumns;

    /// <summary>
    /// Holds the lazily built write cache for the single-item write fast path. Only read and
    /// written by <see cref="SQLiteTable{T}" /> after the model is frozen.
    /// </summary>
    internal TableWriteCache? SingleWriteCache { get; set; }

    internal void AddCompositeForeignKey(ForeignKeyInfo info)
    {
        compositeForeignKeys.Add(info);
    }

    internal void RenameForeignKeyColumnSource(string previousName, string newName)
    {
        for (int i = 0; i < compositeForeignKeys.Count; i++)
        {
            if (compositeForeignKeys[i].Columns.Contains(previousName))
            {
                compositeForeignKeys[i] = RebuildWithRenamedSource(compositeForeignKeys[i], previousName, newName);
            }
        }

        foreach (TableColumn column in columns)
        {
            if (column.ForeignKey is { } foreignKey && foreignKey.Columns.Contains(previousName))
            {
                column.ForeignKey = RebuildWithRenamedSource(foreignKey, previousName, newName);
            }
        }
    }

    internal void RemoveColumn(TableColumn column)
    {
        columns.Remove(column);
    }

    internal void AddComputedColumn(ComputedColumnSpec spec)
    {
        computedColumns.Add(spec);
    }

    internal void AddCheck(CheckConstraintSpec spec)
    {
        checks.Add(spec);
    }

    internal void AddIndex(IndexSpec spec)
    {
        indexes.Add(spec);
    }

    internal void AddTrigger(TriggerSpec spec)
    {
        triggers.Add(spec);
    }

    internal void AddShadowColumn(ShadowColumnSpec spec)
    {
        shadowColumns.Add(spec);
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

    private static ForeignKeyInfo RebuildWithRenamedSource(ForeignKeyInfo foreignKey, string previousName, string newName)
    {
        string[] renamed = foreignKey.Columns.Select(c => c == previousName ? newName : c).ToArray();
        return new ForeignKeyInfo(renamed, foreignKey.TargetTable, foreignKey.TargetColumns, foreignKey.OnDelete, foreignKey.OnUpdate, foreignKey.Deferred);
    }
}
