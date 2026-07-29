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

    internal override IReadOnlyList<(string Column, string ValueSql)> ExtraWriteColumns => extraColumns;

    internal override bool ExtraWriteColumnsReferenceRow => referencesRow;

    protected internal override string WrapParam(string placeholder, TableColumn column)
    {
        return source.WrapParam(placeholder, column);
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

    protected internal override bool RunHooks(IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks, T item)
    {
        return source.RunHooks(hooks, item);
    }

    protected internal override bool RunHooks(IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks, T item, IDictionary<string, object?> columns)
    {
        return source.RunHooks(hooks, item, columns);
    }

    protected internal override SQLiteAction RunActionHooks(T item, SQLiteAction startingAction)
    {
        return source.RunActionHooks(item, startingAction);
    }
}
