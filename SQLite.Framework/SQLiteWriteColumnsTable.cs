namespace SQLite.Framework;

/// <summary>
/// A table view returned by <see cref="SQLiteTable{T}.WithColumns" /> that carries extra column
/// values into the next <c>Add</c> or <c>Update</c>. It behaves like the table it wraps, except the
/// generated <c>INSERT</c> and <c>UPDATE</c> include the extra columns.
/// </summary>
internal sealed class SQLiteWriteColumnsTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T> : SQLiteTable<T>
{
    private readonly IReadOnlyList<(string Column, string ValueSql)> extraColumns;
    private readonly bool referencesRow;

    internal SQLiteWriteColumnsTable(SQLiteDatabase database, TableMapping table, IReadOnlyList<(string Column, string ValueSql)> extraColumns, bool referencesRow)
        : base(database, table)
    {
        this.extraColumns = extraColumns;
        this.referencesRow = referencesRow;
    }

    internal override IReadOnlyList<(string Column, string ValueSql)> ExtraWriteColumns => extraColumns;

    internal override bool ExtraWriteColumnsReferenceRow => referencesRow;
}
