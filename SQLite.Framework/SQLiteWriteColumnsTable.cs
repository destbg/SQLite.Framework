namespace SQLite.Framework;

/// <summary>
/// A table view returned by <see cref="SQLiteTable{T}.WithColumns" /> that carries extra column
/// values into the next <c>Add</c> or <c>Update</c>. It behaves like the table it wraps, except the
/// generated <c>INSERT</c> and <c>UPDATE</c> include the extra columns.
/// </summary>
internal sealed class SQLiteWriteColumnsTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T> : SQLiteTable<T>
{
    private readonly IReadOnlyList<(string Column, string ValueSql)> extraColumns;

    internal SQLiteWriteColumnsTable(SQLiteDatabase database, TableMapping table, IReadOnlyList<(string Column, string ValueSql)> extraColumns)
        : base(database, table)
    {
        this.extraColumns = extraColumns;
    }

    internal override IReadOnlyList<(string Column, string ValueSql)> ExtraWriteColumns => extraColumns;
}
