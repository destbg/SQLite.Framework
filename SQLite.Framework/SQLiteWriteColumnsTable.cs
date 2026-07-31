namespace SQLite.Framework;

/// <summary>
/// A table view returned by <see cref="SQLiteTable{T}.WithColumns" /> that carries extra column
/// values into the next <c>Add</c> or <c>Update</c>. It behaves like the table it wraps, except the
/// generated <c>INSERT</c> and <c>UPDATE</c> include the extra columns. The item level members
/// forward to the wrapped table, so a subclass override of a hook or a binding helper still runs.
/// </summary>
internal sealed class SQLiteWriteColumnsTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T> : SQLiteTable<T>
{
    private readonly SQLiteTable<T> source;
    private readonly IReadOnlyList<(string Column, string ValueSql)> extraColumns;
    private readonly bool referencesRow;

    internal SQLiteWriteColumnsTable(SQLiteTable<T> source, IReadOnlyList<(string Column, string ValueSql)> extraColumns, bool referencesRow)
        : base(source.Database, source.Table)
    {
        this.source = source;
        this.extraColumns = extraColumns;
        this.referencesRow = referencesRow;
    }

    internal override IReadOnlyList<(string Column, string ValueSql)> ExtraWriteColumns
    {
        get
        {
            IReadOnlyList<(string Column, string ValueSql)> inherited = source.ExtraWriteColumns;
            if (inherited.Count == 0)
            {
                return extraColumns;
            }

            List<(string Column, string ValueSql)> combined = new(inherited.Count + extraColumns.Count);
            foreach ((string Column, string ValueSql) entry in inherited)
            {
                if (extraColumns.All(e => !string.Equals(e.Column, entry.Column, StringComparison.OrdinalIgnoreCase)))
                {
                    combined.Add(entry);
                }
            }

            combined.AddRange(extraColumns);
            return combined;
        }
    }

    internal override bool ExtraWriteColumnsReferenceRow => referencesRow || source.ExtraWriteColumnsReferenceRow;

    internal override bool IsItemMethodOverridden(string methodName)
    {
        return source.IsItemMethodOverridden(methodName);
    }

    protected internal override string WrapParam(string placeholder, TableColumn column)
    {
        return source.WrapParam(placeholder, column);
    }

    protected override (TableColumn[] Columns, string Sql) GetAddInfo()
    {
        return source.IsItemMethodOverridden(nameof(GetAddInfo)) ? source.GetAddInfoCore() : base.GetAddInfo();
    }

    protected internal override (TableColumn[] Columns, TableColumn[] PrimaryColumns, string Sql) GetUpdateInfo()
    {
        return source.IsItemMethodOverridden(nameof(GetUpdateInfo)) ? source.GetUpdateInfo() : base.GetUpdateInfo();
    }

    protected internal override (TableColumn[] PrimaryColumns, string Sql) GetRemoveInfo()
    {
        return source.IsItemMethodOverridden(nameof(GetRemoveInfo)) ? source.GetRemoveInfo() : base.GetRemoveInfo();
    }

    protected internal override (TableColumn[] Columns, string Sql) GetAddOrUpdateInfo(SQLiteConflict conflict)
    {
        return source.IsItemMethodOverridden(nameof(GetAddOrUpdateInfo)) ? source.GetAddOrUpdateInfo(conflict) : base.GetAddOrUpdateInfo(conflict);
    }

    protected internal override (TableColumn[] Columns, string Sql) GetUpsertInfo(Action<SQLiteUpsertBuilder<T>> configure)
    {
        return source.IsItemMethodOverridden(nameof(GetUpsertInfo)) ? source.GetUpsertInfo(configure) : base.GetUpsertInfo(configure);
    }

    protected internal override int InsertItem(TableColumn[] columns, string sql, T item, bool detectInsertByRowIdChange = false)
    {
        return source.InsertItem(columns, sql, item, detectInsertByRowIdChange);
    }

    protected internal override int AddOrRemoveItem(TableColumn[] columns, string sql, T item)
    {
        return source.AddOrRemoveItem(columns, sql, item);
    }

    protected internal override int UpdateItem(TableColumn[] columns, TableColumn[] primaryColumns, string sql, T item)
    {
        return source.UpdateItem(columns, primaryColumns, sql, item);
    }

    protected internal override bool RunHooks(IReadOnlyList<SQLiteEntityHook> hooks, T item)
    {
        return source.RunHooks(hooks, item);
    }

    protected internal override bool RunHooks(IReadOnlyList<SQLiteEntityHook> hooks, T item, IDictionary<string, object?> columns)
    {
        return source.RunHooks(hooks, item, columns);
    }

    protected internal override SQLiteAction RunActionHooks(T item, SQLiteAction startingAction)
    {
        return source.RunActionHooks(item, startingAction);
    }
}
